using RSG;
using System.Diagnostics;
using uMicrophoneWebGL;
using UnityEngine;

public class VoiceRecorder : MonoBehaviour
{
    public delegate void AudioClipHandler(AudioClip clip);

    public event AudioClipHandler OnRecordStop;

    [SerializeField] MicrophoneWebGL microphone;

    [Header("Voice Detection")]
    [SerializeField] float noiseFloor = 0.02f;
    [SerializeField] float maxPauseLength = 2f;

    [Header("Debug")]
    [SerializeField] float noiseLevel;
    [SerializeField] float _secondsOfSilence;
    [SerializeField] bool _isRecording;
    [SerializeField] bool _isVoiceDetected;

    float[] _data;
    int _position;
    AudioClip _clip;
    Stopwatch _stopwatch;

    public Promise<AudioClip> Recording { get; set; }

    public bool IsRecording => _isRecording;
    public bool IsVoiceDetected => _isVoiceDetected;
    public float NoiseFloor => noiseFloor;

    public void Record(Promise<AudioClip> recording)
    {
        if (IsRecording) return;
        _isRecording = true;
        _stopwatch.Restart();
        Recording = recording;
        AllocateClip();
        microphone.Begin();
    }

    public void StopRecord()
    {
        if (!IsRecording) return;
        _isRecording = false;
        microphone.End();
        SendDataToEvents();
    }

    public void OnDataReceived(float[] data)
    {
        if (!IsRecording) return;
        data.CopyTo(_data, _position);
        _position += data.Length;
        _isVoiceDetected = DetectVoice(data);
        if (IsVoiceDetected)
            _stopwatch.Restart();
    }

    void Awake()
    {
        microphone.dataEvent.AddListener(OnDataReceived);
        _stopwatch = new Stopwatch();
    }

    void Start()
    {
        _stopwatch.Start();
    }

    void FixedUpdate()
    {
        if (!IsRecording) return;
        _secondsOfSilence = (float)_stopwatch.Elapsed.TotalSeconds;
        if (_secondsOfSilence > maxPauseLength)
            StopRecord();
    }

    void SendDataToEvents()
    {
        CreateClip();
        Recording.Resolve(_clip);
        OnRecordStop?.Invoke(_clip);
    }

    bool DetectVoice(float[] data)
    {
        noiseLevel = 0;
        for (var i = 0; i < data.Length; i++)
            noiseLevel += Mathf.Abs(data[i]);
        noiseLevel /= data.Length;
        return noiseFloor <= noiseLevel;
    }

    void AllocateClip()
    {
        _data = new float[5760000];
    }

    void CreateClip()
    {
        var length = (int)(_data.Length * 48000f / 44100f);
        _clip = AudioClip.Create("Voice", length, 1, 48000, false);
        _clip.SetData(_data, _position = 0);
    }
}