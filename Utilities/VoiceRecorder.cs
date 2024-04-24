using RSG;
using System.Collections.Generic;
using System.Diagnostics;
using uMicrophoneWebGL;
using UnityEngine;

public class VoiceRecorder : MonoBehaviour
{
    public delegate void AudioClipHandler(AudioClip clip);
    public delegate void ReadyHandler(VoiceRecorder recorder);

    public event AudioClipHandler OnRecordStop;
    public event ReadyHandler OnReady;

    [SerializeField]
    private MicrophoneWebGL microphone;

    [Header("Voice Detection")]
    [SerializeField]
    private float maxPauseLength = 2f;
    [SerializeField]
    private float sensitivity = 1.2f;

    private bool hasVoiceBeenDetected;

    private float[] _data;
    private int _position;
    private AudioClip _clip;
    private Device _device;

    private Stopwatch _stopwatch = new Stopwatch();

    public Promise<AudioClip> Recording { get; private set; }
    public Promise<float> Calibrating { get; private set; }
    public float MaxPauseLength => maxPauseLength;

    public float NoiseFloor { get; private set; }
    public float NoiseLevel { get; private set; }
    public float SecondsOfSilence { get; private set; }

    public bool IsCalibrating { get; private set; }
    public bool IsRecording { get; private set; }
    public bool IsVoiceDetected { get; private set; }

    private void Start()
    {
        microphone.readyEvent.AddListener(OnMicrophoneReady);
        microphone.deviceListEvent.AddListener(SetDeviceList);
        microphone.dataEvent.AddListener(OnDataReceived);
        _stopwatch.Start();
        if (microphone.devices?.Count > 0)
            _device = microphone.devices[0];
    }

    private void FixedUpdate()
    {
        if (!IsRecording) return;
        SecondsOfSilence = (float)_stopwatch.Elapsed.TotalSeconds;
        if (SecondsOfSilence > maxPauseLength &&
           (IsCalibrating || hasVoiceBeenDetected))
            StopRecord();
    }

    public IPromise<AudioClip> Record()
    {
        if (IsRecording) return null;
        Recording = new Promise<AudioClip>();
        IsRecording = true;

        PrepareMicrophone();

        return Recording;
    }

    public void StopRecord()
    {
        if (!IsRecording) return;
        IsRecording = false;
        microphone.End();
        SendDataToEvents();
    }

    public IPromise<float> Calibrate()
    {
        if (IsCalibrating) return null;
        IsCalibrating = true;
        Calibrating = new Promise<float>();
        NoiseFloor = 0f;

        PrepareMicrophone();

        return Calibrating.Then(noise => {
            IsCalibrating = false;
            NoiseFloor = noise;
            return NoiseFloor;
        });
    }

    public void PrepareMicrophone()
    {
        _stopwatch.Restart();
        AllocateClip();
        microphone.Begin();
        hasVoiceBeenDetected = false;
        IsRecording = true;
    }

    public void OnDataReceived(float[] data)
    {
        if (!IsRecording) return;
        data.CopyTo(_data, _position);
        _position += data.Length;
        var detected = DetectVoice(data);
        if (IsVoiceDetected && detected)
            _stopwatch.Restart();
        IsVoiceDetected = detected;
        hasVoiceBeenDetected |= IsVoiceDetected;
    }

    public void OnMicrophoneReady()
    {
        Calibrate().Then((noiseFloor) => OnReady?.Invoke(this));
    }

    public void SetDeviceList(List<Device> devices)
    {
        _device = devices[0];
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
            Recording.Resolve(_clip);
            OnRecordStop?.Invoke(_clip);
        }
    }

    private bool DetectVoice(float[] data)
    {
        for (var i = 0; i < data.Length; i++)
            NoiseLevel += Mathf.Abs(data[i]);
        NoiseLevel /= data.Length;

        if (IsCalibrating && NoiseLevel > NoiseFloor)
            NoiseFloor = NoiseLevel * sensitivity;
        else if (IsRecording)
            return NoiseLevel > NoiseFloor;
        return false;
    }

    private void AllocateClip()
    {
        _data = new float[5760000];
    }

    private void CreateClip()
    {
        var length = (int)(_data.Length * (_device.sampleRate / 44100f));
        _clip = AudioClip.Create("Voice", length, 1, _device.sampleRate, false);
        _clip.SetData(_data, _position = 0);
    }
}