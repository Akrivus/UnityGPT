﻿using System;
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
        _secondsOfSilence = (float)_stopwatch.Elapsed.TotalSeconds;
        var voice = CheckVoice(data);
        if (voice && !_isVoiceDetected)
            _secondsOfSilence = 0;
        else if (!voice)
            if (_secondsOfSilence > maxPauseLength)
                StopRecord();
        return voice;
    }

    public void Record()
    {
        if (IsRecording) return;
        _isRecording = true;
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
        var noise = 0.0f;
        for (var i = 0; i < data.Length; i++)
            noise += Mathf.Abs(data[i]);
        noise /= data.Length;
        return noiseFloor <= noise;
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