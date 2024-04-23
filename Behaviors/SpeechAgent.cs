using RSG;
using System;
using System.Collections;
using UnityEngine;

public class SpeechAgent : MonoBehaviour, IChatBehavior
{
    public event Action<string> OnSpeechPlaying;
    public event Action OnSpeechComplete;

    public event Func<string, IPromise<string>> OnTextGenerated;

    public bool IsReady => speaker.IsReady;

    public string Prompt
    {
        get => textGenerator.Context;
        set => textGenerator.Context = value;
    }

    [Header("Text")]
    [SerializeField, TextArea(3, 10)]
    private string prompt = "You are a helpful assistant inside of a Unity scene.";
    [SerializeField]
    private TextModel model = TextModel.GPT_3p5_Turbo;
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

    private StreamingTextGenerator textGenerator;
    private SpeechGenerator speaker;

    private void Awake()
    {
        wordMapping = wordMapping ?? ScriptableObject.CreateInstance<WordMapping>();
        textGenerator = new StreamingTextGenerator(prompt, model, maxTokens, temperature);
        speaker = new SpeechGenerator(textGenerator, wordMapping, TextToSpeechModel.TTS_1,
            voice, pitch);
        speaker.OnTextGenerated += OnTextGenerated;
        speaker.OnSpeechPlaying += OnSpeechPlaying;
        speaker.OnSpeechComplete += OnSpeechComplete;
    }

    public IEnumerator RespondTo(string content, Action<string> callback)
    {
        yield return new WaitUntil(() => speaker.IsReady);
        yield return speaker.RespondTo(content).Then(callback);
        yield return speaker.PlaySpeech(source);
        yield return new WaitUntil(() => speaker.IsReady);
    }
}