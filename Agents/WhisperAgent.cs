using RSG;
using System;
using System.Collections;
using UnityEngine;

public class WhisperAgent : MonoBehaviour, IChatAgent
{
    public event Action<string> OnTextGenerated;

    public bool IsReady { get; set; }

    [SerializeField, TextArea(2, 10)]
    private string prompt;
    [SerializeField, Range(0.0f, 1.0f)]
    private float temperature = 0.5f;
    [SerializeField]
    private VoiceRecorder recorder;

    public WhisperTextGenerator Whisper { get; private set; }

    public IEnumerator RespondTo(string content, Action<string> callback)
    {
        yield return new WaitUntil(() => IsReady);
        IsReady = false;
        yield return Whisper.RespondTo(content).Then(
            SetReady + callback);
        yield return new WaitUntil(() => IsReady);
    }

    private void SetReady(string text = "")
    {
        IsReady = true;
        OnTextGenerated?.Invoke(text);
    }

    private void Awake()
    {
        Whisper = new WhisperTextGenerator(recorder, prompt, temperature);
    }
}
