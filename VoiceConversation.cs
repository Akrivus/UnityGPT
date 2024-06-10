using Proyecto26;
using RSG;
using System;
using System.Collections;
using UnityEngine;

public class VoiceConversation : MonoBehaviour
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

    [SerializeField]
    protected LinkOpenAI client;

    [SerializeField]
    private string model = "gpt-3.5-turbo";

    [SerializeField]
    private string voice = "echo";

    [SerializeField, Range(0.25f, 4.0f)]
    private float speed = 1.0f;

    [SerializeField]
    private int maxTokens = 1024;

    [SerializeField]
    private float temperature = 1f;

    [SerializeField]
    private WordMapping wordMapping;

    [TextArea(6, 18)]
    public string Prompt;

    [SerializeField]
    public string InterstitialPrompt = "{0}";

    [SerializeField]
    protected AudioSource source;

    [SerializeField]
    protected VoiceRecorder recorder;

    protected IStreamingTextGenerator text;
    protected SpeechGenerator speaker;
    protected WhisperTextGenerator whisper;

    private void Awake()
    {
        text = new StreamingTextGenerator(client, Prompt, model, maxTokens, temperature, InterstitialPrompt);
        speaker = new SpeechGenerator(client, text, wordMapping, voice, speed);
        whisper = new WhisperTextGenerator(client, recorder, null, model, maxTokens, temperature);
        wordMapping = wordMapping ?? ScriptableObject.CreateInstance<WordMapping>();
    }

    public IEnumerator Chat()
    {
        yield return new WaitUntil(() => speaker.IsReady);
        yield return whisper.RespondTo(string.Empty).Then((message) => speaker.RespondTo(message));
        yield return new WaitUntil(() => whisper.IsReady);
        yield return speaker.PlaySpeech(source);
        yield return new WaitUntil(() => !source.isPlaying);
        yield return StartCoroutine(Chat());
    }
}