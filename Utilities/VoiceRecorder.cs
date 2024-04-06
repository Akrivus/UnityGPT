using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

public class VoiceRecorder : MonoBehaviour
{
    public delegate void AudioClipHandler(AudioClip clip);

    public event AudioClipHandler OnRecordStop;

    [Header("Voice Detection")]
    [SerializeField] float noiseFloor = 0.02f;
    [SerializeField] float maxPauseLength = 2f;

    [Header("Recording")]
    [SerializeField] DeviceInfo[] devices;
    [SerializeField] int device = 0;

    [Header("Debug")]
    [SerializeField] float noiseLevel;
    [SerializeField] float _secondsOfSilence;
    [SerializeField] bool _isRecording;
    [SerializeField] bool _isVoiceDetected;
    int _position;
    Stopwatch _stopwatch;
    AudioClip _clip;
    DeviceInfo _device;

    public int SecondsOfSilence => (int)_secondsOfSilence;
    public bool IsRecording => _isRecording;
    public bool IsVoiceDetected => _isVoiceDetected;
    public float NoiseFloor => noiseFloor;

    void Awake()
    {
        devices = Microphone.devices.Select(SelectDevice).ToArray();
        _device = devices[device % devices.Length];
        _stopwatch = new Stopwatch();
    }

    void Start()
    {
        _stopwatch.Start();
    }

    void FixedUpdate()
    {
        if (!IsRecording) return;
        var pos = Microphone.GetPosition(_device.Name);
        if (pos - _position < _clip.frequency) return;
        var data = GetDataFrom(pos);
        _position = pos;
        _isVoiceDetected = GetVoiceFrom(data);
        if (IsVoiceDetected)
            _stopwatch.Restart();
    }

    float[] GetDataFrom(int pos)
    {
        var length = Math.Abs(pos - _position);
        if (length < 0) return new float[0];
        var data = new float[length];
        _clip.GetData(data, _position);
        return data;
    }

    bool GetVoiceFrom(float[] data)
    {
        var voice = CheckVoice(data);
        _secondsOfSilence = voice ? 0 : (float)_stopwatch.Elapsed.TotalSeconds;
        if (_secondsOfSilence > maxPauseLength)
            StopRecord();
        return voice;
    }

    public void Record()
    {
        if (IsRecording) return;
        _isRecording = true;
        _stopwatch.Restart();
        _clip = Microphone.Start(_device.Name, false, 120, _device.Frequency);
    }

    public void StopRecord()
    {
        if (!IsRecording) return;
        _isRecording = false;
        Microphone.End(_device.Name);
        SendDataToEvents();
    }

    void SendDataToEvents()
    {
        var data = new float[_clip.samples];
        _clip.GetData(data, 0);
        OnRecordStop?.Invoke(_clip);
    }

    bool CheckVoice(float[] data)
    {
        noiseLevel = 0;
        for (var i = 0; i < data.Length; i++)
            noiseLevel += Mathf.Abs(data[i]);
        noiseLevel /= data.Length;
        return noiseFloor <= noiseLevel;
    }

    DeviceInfo SelectDevice(string name, int i)
    {
        Microphone.GetDeviceCaps(name, out var min, out var freq);
        return new DeviceInfo {
            Name = name,
            Min = min,
            Frequency = freq
        };
    }
}

[Serializable]
public struct DeviceInfo
{
    public string Name;
    public int Min;
    public int Frequency;
}