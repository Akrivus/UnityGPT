using System;
using System.Collections;
using UnityEngine;

public class ChatAgent : AbstractAgent
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

    public override bool IsReady => chatter.IsReady;

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
        message = string.Format("{0}\nNow respond as {1}:", message, name);
        yield return new WaitUntil(() => chatter.IsReady);
        yield return chatter.RespondTo(message).Then(callback);
        yield return chatter.PlaySpeech(source);
        yield return new WaitUntil(() => chatter.IsReady);
    }
}