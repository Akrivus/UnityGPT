using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChatGenerator : MonoBehaviour
{
    public readonly static RestClient API = new RestClient("https://api.openai.com/v1", Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
    readonly static string[] StopCodes = new string[] { ".", "?", "!", "\n" };

    public event EventHandler<ChatEvent> TextGenStart;
    public event EventHandler<ChatEvent> TextGenStep;
    public event EventHandler<ChatEvent> TextGenComplete;

    public event EventHandler<ChatEvent> TalkGenStart;
    public event EventHandler<ChatEvent> TalkGenStep;
    public event EventHandler<ChatEvent> TalkGenComplete;

    public event EventHandler<ChatEvent> ChatGenStart;
    public event EventHandler<ChatEvent> ChatGenStep;
	public event EventHandler<ChatEvent> ChatGenComplete;

    [TextArea(3, 10)]
    [SerializeField] string prompt = "You are a helpful assistant inside of a Unity scene.";
    [SerializeField] string model = "gpt-3.5-turbo";
    [SerializeField] int maxTokens = 4096;
    [SerializeField] float temperature = 1.0f;
    [SerializeField] GenerateTextToSpeech.Voices voice = GenerateTextToSpeech.Voices.Onyx;
    [SerializeField] AudioSource source;
    [SerializeField, Range(0, 2)] float pitch = 1.0f;
    [SerializeField] WordMap phonetics;
    [SerializeField] string message;

    TextGenerator text;
    TextToSpeechGenerator textToSpeech;

    Queue<TextToSpeech> lines = new Queue<TextToSpeech>();

    string _text;
    TextToSpeech _tts;
    States _state;

    public bool IsReady { get; private set; }
    public bool IsTexting
    {
        get => States.Texting == _state;
        private set
        {
            var handler = value ? TextGenStart : TextGenComplete;
            _state =  value ? States.Texting : States.Waiting;
            handler?.Invoke(this, new ChatEvent(_state, message));
            if (value) _text = string.Empty;
        }
    }
    public bool IsTalking
    {
        get => States.Talking == _state;
        private set
        {
            var handler = value ? TalkGenStart : TalkGenComplete;
            _state = value ? States.Talking : States.Complete;
            handler?.Invoke(this, new ChatEvent(_state, message));
        }
    }
    public bool IsWaiting
    {
        get => States.Waiting == _state;
        private set => _state = value ? States.Waiting : _state;
    }
    public bool IsComplete
    {
        get => States.Complete == _state;
        private set
        {
            var handler = value ? ChatGenComplete : ChatGenStart;
            _state = value ? States.Complete : States.Waiting;
            if (value) IsReady = value;
            ChatGenComplete?.Invoke(this, new ChatEvent(_state, message));
        }
    }

    public bool IsSpeaking => lines.Count > 0 || source.isPlaying;
    public bool IsGenerating => IsTexting || IsTalking;

    void Awake()
    {
        text = new TextGenerator(prompt, model, maxTokens, temperature);
        text.NextTextToken += OnNextToken;
        text.TextComplete += OnTextCompleted;
        textToSpeech = new TextToSpeechGenerator(voice);

        if (!string.IsNullOrEmpty(message))
            TellMe(message);
        if (phonetics == null)
            phonetics = ScriptableObject.CreateInstance<WordMap>();
    }

    void Update()
    {
        if (IsWaiting && !IsGenerating && !IsReady)
            IsComplete = true;
		if (IsTalking && !IsSpeaking)
			IsTalking = false;
        if (lines.TryPeek(out var tts))
        {
            if (!tts.IsReady || source.isPlaying)
                return;
            TalkGenStep?.Invoke(this,
                new ChatEvent(States.Talking, message, tts.Text));
            source.pitch = pitch;
            source.clip = tts.Speech;
            source.Play();
            lines.Dequeue();
        }
    }

    void OnNextToken(object sender, Choice.Chunk e)
    {
        var content = e.Content ?? string.Empty;
        _text += content;
        var matches = false;
        foreach (var code in StopCodes)
            if (content.Contains(code))
                matches = true;
        if (!matches)
            return;
        message += _text;
        _tts = new TextToSpeech(phonetics.Filter(_text));
        lines.Enqueue(_tts);
        textToSpeech.GenerateSpeech(_tts);
        TextGenStep?.Invoke(this,
            new ChatEvent(States.Texting, message, _text));
        _text = string.Empty;
    }

    void OnTextCompleted(object sender, Message message)
    {
        IsTexting = false;
		IsTalking = true;
        ChatGenStep?.Invoke(this,
            new ChatEvent(States.Talking, message.Content));
    }

    public IEnumerator GenerateChat(string content)
    {
        message = string.Empty;
        IsTexting = true;
        IsReady = false;
        yield return text.GenerateText(content);
    }

    public void TellMe(string content)
    {
        StartCoroutine(GenerateChat(content));
    }

    public enum States
    {
        Texting, Talking, Waiting, Complete
    }
}

public class ChatEvent : EventArgs
{
    public ChatGenerator.States State { get; set; }
    public string Message { get; set; }
    public string Segment { get; set; }

    public ChatEvent(ChatGenerator.States state, string message, string segment) : this(state, message)
    {
        Segment = segment;
    }

    public ChatEvent(ChatGenerator.States state, string message)
    {
        State = state;
        Message = message;
    }
}