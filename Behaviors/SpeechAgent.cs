using RSG;
using System;
using System.Collections;
using UnityEngine;

public class SpeechAgent : MonoBehaviour, IChatAgent
{
    public event Action<string> OnSpeechPlaying
    {
        add => speaker.OnSpeechPlaying += value;
        remove => speaker.OnSpeechPlaying -= value;
    }

    public event Action OnSpeechComplete
    {
        add => speaker.OnSpeechComplete += value;
        remove => speaker.OnSpeechComplete -= value;
    }

    public event Action<string> OnTextGenerated
    {
        add => speaker.OnStreamEnded += value;
        remove => speaker.OnStreamEnded -= value;
    }

    public event Func<string, IPromise<string>> OnWhisperGenerated
    {
        add => whisper.OnTextGenerated += value;
        remove => whisper.OnTextGenerated -= value;
    }

    public event Action<ProxySession> OnSuccessfulLink;

    protected IStreamingTextGenerator text;
    protected SpeechGenerator speaker;
    protected WhisperTextGenerator whisper;

    [Header("Chat")]
    [SerializeField]
    protected PhrenProxyClient client;
    [SerializeField]
    protected Roles role = Roles.System;

    [Header("Speech")]
    [SerializeField, Range(0.8f, 1.2f)]
    protected float pitch = 1.0f;
    [SerializeField]
    protected AudioSource source;
    [SerializeField]
    protected WordMapping wordMapping;
    [SerializeField]
    protected string interstitialPrompt = "{0}";

    public virtual bool IsReady => speaker.IsReady;

    private void Awake()
    {
        client.OnSuccessfulLink += Link;
    }

    public virtual IEnumerator RespondTo(string message, Action<string> callback)
    {
        message = string.Format(interstitialPrompt, message, name);
        yield return new WaitUntil(() => speaker.IsReady);
        yield return speaker.RespondTo(message).Then(callback);
        yield return speaker.PlaySpeech(source);
        yield return new WaitUntil(() => speaker.IsReady);
    }

    public virtual void Link(ProxySession session)
    {
        wordMapping = wordMapping ?? ScriptableObject.CreateInstance<WordMapping>();
        text = new StreamingTextGenerator(client, session.Messages, session.Model, session.MaxTokens, session.Temperature, session.InterstitialPrompt);
        speaker = new SpeechGenerator(client, text, wordMapping, session.Voice, pitch, role);

        interstitialPrompt = session.InterstitialPrompt;

        DispatchSuccessfulLink(session);
    }

    protected void DispatchSuccessfulLink(ProxySession session)
    {
        OnSuccessfulLink?.Invoke(session);
    }
}