﻿using RSG;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using uMicrophoneWebGL;
using UnityEngine;

public class VoiceRecorder : MonoBehaviour
{
    public delegate void AudioClipHandler(float[] clip);
    public delegate void ReadyHandler(VoiceRecorder recorder);

    public event AudioClipHandler OnRecordStop;
    public event ReadyHandler OnReady;

    [SerializeField]
    private MicrophoneWebGL microphone;

    [Header("Voice Detection")]
    [SerializeField]
    private float maxPauseLength = 2f;
    [SerializeField]
    private float exponent = 10f;

    private bool hasVoiceBeenDetected;

    private float[] _data;
    private int _position;

    private Stopwatch _stopwatch = new Stopwatch();

    public Promise<float[]> Recording { get; private set; }
    public Promise<float> Calibrating { get; private set; }
    public float MaxPauseLength => maxPauseLength;

    public float NoiseFloor { get; private set; }
    public float NoiseLevel { get; private set; }
    public float SecondsOfSilence { get; private set; }

    public bool IsCalibrating { get; private set; }
    public bool IsRecording { get; private set; }
    public bool IsVoiceDetected { get; private set; }

    public int Frequency => microphone.selectedDevice.sampleRate;
    public int Channels => 1;

    private void Start()
    {
        microphone.readyEvent.AddListener(OnMicrophoneReady);
        microphone.deviceListEvent.AddListener(SetDeviceList);
        microphone.dataEvent.AddListener(OnDataReceived);
        _stopwatch.Start();
    }

    private void FixedUpdate()
    {
        if (!IsRecording) return;
        SecondsOfSilence = (float)_stopwatch.Elapsed.TotalSeconds;
        if (SecondsOfSilence > maxPauseLength &&
           (IsCalibrating || hasVoiceBeenDetected))
            StopRecord();
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
        AllocateClip();
        microphone.Begin();
        hasVoiceBeenDetected = false;
        IsRecording = true;
        _stopwatch.Restart();
    }

    public void OnDataReceived(float[] data)
    {
        if (!IsRecording) return;

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

    public void OnMicrophoneReady()
    {
        Calibrate().Then((noiseFloor) => OnReady?.Invoke(this));
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
}