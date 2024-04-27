using System;
using System.Collections;
using UnityEngine;

public class SpeechAgent : AbstractAgent
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

    public override bool IsReady => speaker.IsReady;

    public string Prompt
    {
        get => textGenerator.Context;
        set => textGenerator.Context = value;
    }

    [Header("Text")]
    [SerializeField, TextArea(3, 10)]
    private string prompt = "You are a helpful assistant inside of a Unity scene.";
    [SerializeField]
    private TextModel model = TextModel.GPT_3_Turbo;
    [SerializeField]
    private int maxTokens = 4096;
    [SerializeField, Range(0.1f, 1.0f)]
    private float temperature = 1.0f;

    [Header("Speech")]
    [SerializeField]
    private GenerateTextToSpeech.Voices voice = GenerateTextToSpeech.Voices.Onyx;
    [SerializeField]
    private WordMapping wordMapping;
    [SerializeField, Range(0.8f, 1.2f)]
    private float pitch = 1.0f;
    [SerializeField]
    private AudioSource source;

    private IStreamingTextGenerator textGenerator;
    private SpeechGenerator speaker;

    private void Awake()
    {
        wordMapping = wordMapping ?? ScriptableObject.CreateInstance<WordMapping>();
        textGenerator = new StreamingTextGenerator(prompt, model, maxTokens, temperature);
        speaker = new SpeechGenerator(textGenerator, wordMapping, TextToSpeechModel.TTS_1, voice, pitch);
    }

    public override IEnumerator RespondTo(string content, Action<string> callback)
    {
        content = string.Format("{0}\nNow respond as {1}:", content, name);
        yield return new WaitUntil(() => speaker.IsReady);
        yield return speaker.RespondTo(content).Then(callback);
        yield return speaker.PlaySpeech(source);
        yield return new WaitUntil(() => speaker.IsReady);
    }
}