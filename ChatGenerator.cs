using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChatGenerator : MonoBehaviour
{
    public readonly static RestClient API = new RestClient("https://api.openai.com/v1", Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
    readonly static string[] StopCodes = new string[] { ".", "?", "!", "\n" };

    public event EventHandler<string> TextStarted;
	public event EventHandler<string> TextCompleted;
	public event EventHandler<string> TalkStarted;
	public event EventHandler<string> TalkCompleted;
	public event EventHandler<string> ChatCompleted;

    [SerializeField] string prompt = "You are a helpful assistant inside of a Unity scene.";
    [SerializeField] string model = "gpt-3.5-turbo";
    [SerializeField] int maxTokens = 4096;
    [SerializeField] float temperature = 1.0f;
    [SerializeField] GenerateTextToSpeech.Voices voice = GenerateTextToSpeech.Voices.Onyx;
    [SerializeField] AudioSource source;

    TextGenerator text;
    TextToSpeechGenerator textToSpeech;

    Queue<TextToSpeech> lines = new Queue<TextToSpeech>();

    string sentence;
    public string message;

    public bool IsTexting { get; private set; }
    public bool IsTalking { get; private set; }
    public bool IsWaiting { get; private set; }
    public bool IsReady { get; private set; }

    public IEnumerator GenerateChat(string content)
    {
        IsTexting = true;
        IsTalking = false;
        IsWaiting = false;
        IsReady = false;
        sentence = string.Empty;
        message = string.Empty;
		TextStarted?.Invoke(this, content);
        yield return text.GenerateText(content);
    }

    void Awake()
    {
        text = new TextGenerator(prompt, model, maxTokens, temperature);
        text.NextToken += OnNextToken;
        text.TextCompleted += OnTextCompleted;
        textToSpeech = new TextToSpeechGenerator(voice);
    }

    void Update()
    {
        if (IsWaiting && !IsTalking && !IsReady)
        {
            ChatCompleted?.Invoke(this, message);
            IsReady = true;
        }
		if (IsTalking && lines.Count == 0 && !source.isPlaying)
		{
			TalkCompleted?.Invoke(this, message);
			IsTalking = false;
		}
        if (lines.TryPeek(out var tts))
        {
            if (!tts.Ready || source.isPlaying)
                return;
            tts.Play(source);
            lines.Dequeue();
        }
    }

    void OnNextToken(object sender, Choice.Chunk e)
    {
        var content = e.Content ?? string.Empty;
        sentence += content;
        var matches = false;
        foreach (var code in StopCodes)
            if (content.Contains(code))
                matches = true;
        if (!matches) return;
        message += sentence;
        var tts = new TextToSpeech(sentence);
        lines.Enqueue(tts);
        textToSpeech.GenerateSpeech(tts);
        sentence = string.Empty;
    }

    void OnTextCompleted(object sender, string e)
    {
        IsTexting = false;
		IsTalking = true;
        IsWaiting = true;
		TextCompleted?.Invoke(this, message);
		TalkStarted?.Invoke(this, message);
    }
}