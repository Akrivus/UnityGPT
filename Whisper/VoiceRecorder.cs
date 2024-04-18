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

    [SerializeField] MicrophoneWebGL microphone;

    [Header("Voice Detection")]
    [SerializeField] float noiseFloor = 0.02f;
    [SerializeField] float maxPauseLength = 2f;
    [SerializeField] float sensitivity = 1.2f;

    [Header("Debug")]
    [SerializeField] float noiseLevel;
    [SerializeField] float secondsOfSilence;
    bool hasVoiceBeenDetected;

    float[] _data;
    int _position;
    AudioClip _clip;
    Stopwatch _stopwatch;
    Device _device;

    public Promise<AudioClip> Recording { get; private set; }
    public Promise<float> Calibrating { get; private set; }

    public VoiceRecorderUI UI { get; private set; }
    public bool IsPaused { get; set; }

    public float NoiseFloor => noiseFloor;
    public float NoiseLevel => noiseLevel;
    public float SecondsOfSilence => secondsOfSilence;
    public float MaxPauseLength => maxPauseLength;

    public bool IsCalibrating { get; private set; }
    public bool IsRecording { get; private set; }
    public bool IsVoiceDetected { get; private set; }

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
        if (IsRecording) return null;
        IsCalibrating = true;
        Calibrating = new Promise<float>();
        noiseFloor = 0f;

        PrepareMicrophone();

        return Calibrating.Then(noise => {
            IsCalibrating = false;
            noiseFloor = noise;
            return noiseFloor;
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

    void Awake()
    {
        _stopwatch = new Stopwatch();
        UI = GetComponent<VoiceRecorderUI>();
    }

    void Start()
    {
        microphone.readyEvent.AddListener(OnMicrophoneReady);
        microphone.deviceListEvent.AddListener(SetDeviceList);
        microphone.dataEvent.AddListener(OnDataReceived);
        _stopwatch.Start();
        if (microphone.devices?.Count > 0)
            _device = microphone.devices[0];
    }

    void FixedUpdate()
    {
        if (!IsRecording) return;
        secondsOfSilence = (float)_stopwatch.Elapsed.TotalSeconds;
        if (secondsOfSilence > maxPauseLength &&
           (IsCalibrating || hasVoiceBeenDetected))
            StopRecord();
    }

    void SendDataToEvents()
    {
        if (IsCalibrating)
        {
            IsCalibrating = false;
            Calibrating.Resolve(noiseLevel);
        }
        else
        {
            CreateClip();
            Recording.Resolve(_clip);
            OnRecordStop?.Invoke(_clip);
        }
    }

    bool DetectVoice(float[] data)
    {
        for (var i = 0; i < data.Length; i++)
            noiseLevel += Mathf.Abs(data[i]);
        noiseLevel /= data.Length;

        if (IsCalibrating && noiseLevel > noiseFloor)
            noiseFloor = noiseLevel * sensitivity;
        else if (IsRecording)
            return noiseLevel > noiseFloor;
        return false;
    }

    void AllocateClip()
    {
        _data = new float[5760000];
    }

    void CreateClip()
    {
        var length = (int)(_data.Length * (_device.sampleRate / 44100f));
        _clip = AudioClip.Create("Voice", length, 1, _device.sampleRate, false);
        _clip.SetData(_data, _position = 0);
    }
}