using RSG;
using System;
using System.Diagnostics;
using UnityEngine;

#if MICROPHONE_WEBGL && UNITY_WEBGL
using System.Collections.Generic;
using uMicrophoneWebGL;
#endif

public class VoiceRecorder : MonoBehaviour
{
    public delegate void AudioClipHandler(float[] clip);
    public delegate void ReadyHandler(VoiceRecorder recorder);

    public event AudioClipHandler OnRecordStop;
    public event ReadyHandler OnReady;

#if MICROPHONE_WEBGL && UNITY_WEBGL
    [SerializeField]
    private MicrophoneWebGL microphone;
#else
    private AudioClip _clip;
#endif

    [Header("Voice Detection")]
    [SerializeField]
    private int frameSize = 512;
    [SerializeField]
    private float maxPauseLength = 2f;
    [SerializeField]
    private float calibrationTime = 0.5f;
    [SerializeField]
    private float exponent = 8f;
    [SerializeField]
    private float divisor = 2f;

    private bool hasVoiceBeenDetected;

    private float[] _data;
    private int _position;

    private Stopwatch _stopwatch = new Stopwatch();

    public Promise<float[]> Recording { get; private set; }
    public Promise<float> Calibrating { get; private set; }
    public float MaxPauseLength => IsCalibrating ? calibrationTime : maxPauseLength;

    public float ZeroCrossings;
    public float AverageZeroCrossings;
    public float NoiseFloor;
    public float NoiseLevel;
    public float SecondsOfSilence { get; private set; }

    public bool IsCalibrating { get; private set; }
    public bool IsRecording { get; private set; }
    public bool IsVoiceDetected { get; private set; }

#if MICROPHONE_WEBGL
    public int Frequency => microphone.selectedDevice.sampleRate;
    public int Channels => microphone.selectedDevice.channelCount;
#else
    public int Frequency { get; private set; } = 48000;
    public int Channels { get; private set; } = 1;
#endif

    private void Start()
    {
        _stopwatch.Start();
#if MICROPHONE_WEBGL
        microphone.readyEvent.AddListener(OnMicrophoneReady);
        microphone.deviceListEvent.AddListener(SetDeviceList);
        microphone.dataEvent.AddListener(OnDataReceived);
    }

    public void SetDeviceList(List<Device> devices)
    {
        for (int i = 0; i < devices.Count; i++)
            if (devices[i].deviceId.Contains("Default"))
                microphone.micIndex = i;
#else
        Microphone.GetDeviceCaps(null, out var min, out var max);
        Frequency = max;
        OnMicrophoneReady();
#endif
    }

    private void FixedUpdate()
    {
        if (!IsRecording) return;
        SecondsOfSilence = (float)_stopwatch.Elapsed.TotalSeconds;
        if (SecondsOfSilence > MaxPauseLength &&
           (IsCalibrating || hasVoiceBeenDetected))
            StopRecord();
#if MICROPHONE_WEBGL
#else
        if (_clip == null) return;
        var pos = Microphone.GetPosition(null);
        if (pos - _position < _clip.frequency) return;
        OnDataReceived(GetDataFrom(pos));
    }

    private float[] GetDataFrom(int pos)
    {
        var length = Math.Abs(pos - _position);
        if (length < 0) return new float[0];
        var data = new float[length];
        _clip.GetData(data, _position);
        return data;
#endif
    }

    public void OnMicrophoneReady()
    {
        Calibrate().Then((noiseFloor) => OnReady?.Invoke(this));
    }

    public void PrepareMicrophone()
    {
        hasVoiceBeenDetected = false;
        IsRecording = true;
        AllocateClip();
#if MICROPHONE_WEBGL
        microphone.Begin();
#else
        _clip = Microphone.Start(null, true, 120, Frequency);
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
#if MICROPHONE_WEBGL
        microphone.End();
#else
        Microphone.End(null);
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
        NoiseLevel = CalculateNoiseLevel(data);
        ZeroCrossings = CalculateZeroCrossings(data, frameSize);

        if (IsCalibrating)
        {
            if (NoiseLevel > NoiseFloor)
                NoiseFloor = NoiseLevel * exponent;
            AverageZeroCrossings += ZeroCrossings;
            AverageZeroCrossings /= divisor;
        }
        else if (IsRecording)
            return NoiseLevel > NoiseFloor
                && ZeroCrossings > AverageZeroCrossings;

        return false;
    }

    private float CalculateNoiseLevel(float[] data)
    {
        var sum = 0f;
        for (var i = 0; i < data.Length; i++)
            sum += Mathf.Abs(data[i]);
        return sum / data.Length;
    }

    private float CalculateZeroCrossings(float[] data, int frameSize)
    {
        var frames = data.Length / frameSize;
        var total = 0f;
        for (var i = 0; i < frames; i++)
        {
            var frame = new float[frameSize];
            Array.Copy(data, i * frameSize, frame, 0, frameSize);
            total += CountZeroCrossings(frame);
        }
        return total / frames / frameSize;
    }

    private int CountZeroCrossings(float[] data)
    {
        var count = 0;
        for (var i = 1; i < data.Length; i++)
            if ((data[i - 1] >= 0 && data[i] < 0) || (data[i - 1] < 0 && data[i] >= 0))
                count++;
        return count;
    }

    private void AllocateClip()
    {
        _position = 0;
        _data = new float[120 * Frequency * Channels];
    }

    private void CreateClip()
    {
        var data = new float[_position];
#if MICROPHONE_WEBGL
        Array.Copy(_data, data, _position);
#else
        _clip.GetData(data, 0);
#endif
        _data = data;
    }
}