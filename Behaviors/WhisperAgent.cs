using System;
using UnityEngine;
using System.Collections;

public class WhisperAgent : SpeechAgent
{
    public override bool IsReady => _isWhisperReady;

    [Header("Whisper")]
    [SerializeField]
    protected VoiceRecorder recorder;
    [SerializeField]
    protected SpeechAgent listener;

    [Header("Context")]
    [SerializeField]
    private string model = "gpt-3.5-turbo";
    [SerializeField]
    private int maxTokens = 128;
    [SerializeField, Range(0.0f, 1.0f)]
    private float temperature = 1.0f;

    private bool _isWhisperReady = true;

    private void Start()
    {
        if (listener != null)
            listener.OnSuccessfulLink += (chat) => listener.OnTextGenerated += SetContext;
    }

    public override IEnumerator RespondTo(string message, Action<string> callback)
    {
        yield return new UnityEngine.WaitUntil(() => _isWhisperReady);
        _isWhisperReady = false;
        yield return whisper.RespondTo(message).Then(SetReady + callback);
        yield return new UnityEngine.WaitUntil(() => _isWhisperReady);
    }

    public override void Link(SessionData session)
    {
        whisper = new WhisperTextGenerator(client, recorder, session.Description, model, maxTokens, temperature, role);
        DispatchSuccessfulLink(session);
    }

    private void SetReady(string response)
    {
        _isWhisperReady = true;
    }

    private void SetContext(string text)
    {
        whisper.SetContext(text);
    }
}