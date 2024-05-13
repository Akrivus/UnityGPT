using RSG;
using System.Collections;
using System.Diagnostics;
using UnityEngine;

#if UNITY_WEBGL
using System;
using System.Collections.Generic;
using uMicrophoneWebGL;
#endif

public class VoiceRecorder : MonoBehaviour
{
    public delegate void AudioClipHandler(float[] clip);
    public delegate void ReadyHandler(VoiceRecorder recorder);

    public event AudioClipHandler OnRecordStop;
    public event ReadyHandler OnReady;

#if UNITY_WEBGL
    [SerializeField]
    private MicrophoneWebGL microphone;
#else
    private AudioClip _clip;
#endif

    [Header("Voice Detection")]
    [SerializeField]
    private float maxPauseLength = 2f;
    [SerializeField]
    private float calibrationTime = 0.5f;
    [SerializeField]
    private float exponent = 10f;

    private bool hasVoiceBeenDetected;

    private float[] _data;
    private int _position;

    private Stopwatch _stopwatch = new Stopwatch();

    public Promise<float[]> Recording { get; private set; }
    public Promise<float> Calibrating { get; private set; }
    public float MaxPauseLength => IsCalibrating ? calibrationTime : maxPauseLength;

    public float NoiseFloor { get; private set; }
    public float NoiseLevel { get; private set; }
    public float SecondsOfSilence { get; private set; }

    public bool IsCalibrating { get; private set; }
    public bool IsRecording { get; private set; }
    public bool IsVoiceDetected { get; private set; }

    public int Frequency
    {
        get
        {
#if UNITY_WEBGL
            return microphone.selectedDevice.sampleRate;
#else
            Microphone.GetDeviceCaps(null, out var min, out var max);
            return max;
#endif
        }
    }

    public int Channels => 1;

    private void Start()
    {
#if UNITY_WEBGL
        microphone.readyEvent.AddListener(OnMicrophoneReady);
        microphone.deviceListEvent.AddListener(SetDeviceList);
        microphone.dataEvent.AddListener(OnDataReceived);
#else
        StartCoroutine(CalibrateThenSetReady());
#endif
        _stopwatch.Start();
    }

    private void FixedUpdate()
    {
        if (!IsRecording) return;
        SecondsOfSilence = (float)_stopwatch.Elapsed.TotalSeconds;
        if (SecondsOfSilence > MaxPauseLength &&
           (IsCalibrating || hasVoiceBeenDetected))
            StopRecord();
#if !UNITY_WEBGL
        OnDataReceived(GetData());
#endif

    }

    public void PrepareMicrophone()
    {
        hasVoiceBeenDetected = false;
        IsRecording = true;
#if UNITY_WEBGL
        AllocateClip();
        microphone.Begin();
#else
        _clip = Microphone.Start(null, true, 1, Frequency);
#endif
    }

    public IPromise<float> Calibrate()
    {
        if (IsCalibrating) return null;
        IsCalibrating = true;
        Calibrating = new Promise<float>();
        NoiseFloor = 0.002f;

        PrepareMicrophone();

        return Calibrating.Then(noise => {
            IsCalibrating = false;
            NoiseFloor = noise;
            return NoiseFloor;
        });
    }

    public IPromise<float[]> Record()
    {
        if (IsRecording) return null;
        Recording = new Promise<float[]>();
        PrepareMicrophone();

        return Recording;
    }

    public void StopRecord()
    {
        if (!IsRecording) return;
        IsRecording = false;
        _stopwatch.Stop();
        _stopwatch.Reset();
#if UNITY_WEBGL
        microphone.End();
#endif
        SendDataToEvents();
    }

    public void OnDataReceived(float[] data)
    {
        if (!IsRecording) return;
        if (!_stopwatch.IsRunning)
            _stopwatch.Start();

        var detected = DetectVoice(data);
        if (IsCalibrating) return;

        for (var i = 0; i < data.Length; i++)
            _data[_position + i] = data[i];
        _position += data.Length;

        if (IsVoiceDetected && detected)
            _stopwatch.Restart();
        IsVoiceDetected = detected;
        hasVoiceBeenDetected |= IsVoiceDetected;
    }

    private void SendDataToEvents()
    {
        if (IsCalibrating)
        {
            IsCalibrating = false;
            Calibrating.Resolve(NoiseFloor);
        }
        else
        {
            CreateClip();
            Recording.Resolve(_data);
            OnRecordStop?.Invoke(_data);
        }
    }

    private bool DetectVoice(float[] data)
    {
        for (var i = 0; i < data.Length; i++)
            NoiseLevel += Mathf.Abs(data[i]);
        NoiseLevel /= data.Length;

        if (IsCalibrating && NoiseLevel > NoiseFloor)
            NoiseFloor = NoiseLevel * exponent;
        else if (IsRecording)
            return NoiseLevel > NoiseFloor;
        return false;
    }

#if UNITY_WEBGL
    public void OnMicrophoneReady()
    {
        Calibrate().Then((noiseFloor) => OnReady?.Invoke(this));
    }

    private void AllocateClip()
    {
        _position = 0;
        _data = new float[120 * Frequency];
    }

    private void CreateClip()
    {
        var data = new float[_position];
        Array.Copy(_data, data, _position);
        _data = data;
    }

    public void SetDeviceList(List<Device> devices)
    {
        for (int i = 0; i < devices.Count; i++)
        {
            var device = devices[i];
            if (device.deviceId.Contains("Default"))
                microphone.micIndex = i;
        }
    }
#else
    public void CreateClip()
    {
        _data = new float[_clip.samples];
        _clip.GetData(_data, 0);
    }

    public float[] GetData()
    {
        var pos = Microphone.GetPosition(null);
        var length = pos - _position;
        if (length < 0) return new float[0];
        var data = new float[length];
        _clip.GetData(data, _position);
        return data;
    }

    private IEnumerator CalibrateThenSetReady()
    {
        yield return Calibrate();
        OnReady?.Invoke(this);
    }
#endif
}