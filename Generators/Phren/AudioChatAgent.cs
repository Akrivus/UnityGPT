using System;
using System.Collections;
using UnityEngine;

public class AudioChatAgent : AbstractAgent
{
    public event Action<string> OnSpeechPlaying
    {
        add => chatter.OnSpeechPlaying += value;
        remove => chatter.OnSpeechPlaying -= value;
    }
    public event Action OnSpeechComplete
    {
        add => chatter.OnSpeechComplete += value;
        remove => chatter.OnSpeechComplete -= value;
    }

    public event Action<string> OnTextGenerated
    {
        add => chatter.OnStreamEnded += value;
        remove => chatter.OnStreamEnded -= value;
    }

    [Header("Whisper")]
    [SerializeField, TextArea(2, 10)]
    private string prompt;
    [SerializeField]
    private VoiceRecorder recorder;

    [Header("Chat")]
    [SerializeField]
    private string personId;
    [SerializeField, Range(0.8f, 1.2f)]
    private float pitch = 1.0f;
    [SerializeField]
    private AudioSource source;

    private ChatGenerator chatter;

    private void Awake()
    {
        chatter = new ChatGenerator(personId);
    }

    public override IEnumerator RespondTo(string message, Action<string> callback)
    {
        yield return new WaitUntil(() => IsReady && chatter.IsReady);
        yield return chatter.RecordAndRespondTo(recorder, message).Then(callback);
        yield return chatter.PlaySpeech(source);
        yield return new WaitUntil(() => IsReady && chatter.IsReady);
    }
}