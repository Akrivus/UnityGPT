using RSG;
using System;
using System.Collections.Generic;
using UnityEngine;

public class TextToSpeechAgent : MonoBehaviour, IText, ITextToSpeech, IToolCaller
{
    readonly static IPromise<string> TextAgentBusy = Promise<string>.Rejected(new Exception("Text generation is already in progress."));
    readonly static string[] StopCodes = new string[] { ".", "?", "!", "\n" };

    public event EventHandler<TextEventArgs> OnTextStreamTokenReceived;
    public event EventHandler<TextEventArgs> OnTextGenerated;
    public event EventHandler<TextToSpeechEventArgs> OnSpeechGenerated;
    public event EventHandler<TextToSpeechEventArgs> OnSpeechPlayed;
    public event EventHandler<MessageEventArgs> OnMessage;

    public string Prompt
    {
        get => text.Prompt;
        set => text.Prompt = value;
    }

    [Header("Text Generation")]
    [TextArea(3, 10)]
    [SerializeField] string prompt = "You are a helpful assistant inside of a Unity scene.";
    [SerializeField] TextModel model = TextModel.GPT35_Turbo;
    [SerializeField] int maxTokens = 4096;
    [SerializeField, Range(0.1f, 1.0f)] float temperature = 1.0f;

    [Header("Text To Speech")]
    [SerializeField] GenerateTextToSpeech.Voices voice = GenerateTextToSpeech.Voices.Onyx;
    [SerializeField] WordMapping wordMapping;
    [SerializeField] AudioSource source;
    [SerializeField, Range(0.8f, 1.2f)] float pitch = 1.0f;

    TextAgent text;
    TextToSpeechGenerator textToSpeech;

    Queue<TextToSpeech> lines = new Queue<TextToSpeech>();
    TextToSpeech line;
    string _buffer;

    public bool IsGeneratingText { get; private set; }
    public bool IsGeneratingSpeech { get; private set; }
    public Promise<bool> TextToSpeechComplete { get; private set; }

    public IPromise<TextToSpeech> Say(string text)
    {
        return textToSpeech.Say(text);
    }

    public void Tell(string content)
    {
        text.Tell(content);
    }

    public IPromise<string> Ask(string content)
    {
        if (IsGeneratingText) return TextAgentBusy;
        Tell(content);
        return Listen();
    }

    public IPromise<string> Ask(string content, Action<string> tokenCallback)
    {
        if (IsGeneratingText) return TextAgentBusy;
        Tell(content);
        return Listen(tokenCallback);
    }

    public IPromise<string> Listen()
    {
        if (IsGeneratingText) return TextAgentBusy;
        return Listen((token) => DispatchMessageTextToken(token));
    }

    public IPromise<string> Listen(Action<string> tokenCallback)
    {
        if (IsGeneratingText) return TextAgentBusy;
        TextToSpeechComplete = new Promise<bool>();
        return text.Listen(tokenCallback)
            .Then((text) => TextToSpeechComplete
            .Then((complete) => DispatchMessageText(text)));
    }

    public void ResetContext()
    {
        text.ResetContext();
    }

    public void AddTool(params IToolCall[] tools)
    {
        text.AddTool(tools);
    }

    public void RemoveTool(params string[] names)
    {
        text.RemoveTool(names);
    }

    public IPromise<string> Execute(string toolChoice, string input = "")
    {
        return text.Execute(toolChoice, input);
    }

    void Awake()
    {
        textToSpeech = new TextToSpeechGenerator(TextToSpeechModel.TTS_1, voice);
        textToSpeech.OnGenerated += OnGeneratedSpeechReceived;
        text = new TextAgent(model, maxTokens, temperature, GetComponents<IToolCall>());
        text.OnGeneratedTextReceived += OnGeneratedTextReceived;
        text.OnGeneratedTextStreamReceived += OnGeneratedTextStreamReceived;
        text.OnGeneratedTextStreamEnded += OnGeneratedTextStreamEnded;
        text.Prompt = prompt;
        source.pitch = pitch;
    }

    void Update()
    {
        if (IsGeneratingSpeech && source.isPlaying) return;
        if (lines.Count > 0)
            PlayTextToSpeech(lines.Dequeue());
        else if (IsGeneratingSpeech)
        {
            IsGeneratingSpeech = false;
            TextToSpeechComplete.Resolve(true);
        }
    }

    private void ResetTextToSpeechState()
    {
        if (line == null) return;
        line.Play.Resolve(true);
        OnSpeechPlayed?.Invoke(this, new TextToSpeechEventArgs(line));
    }

    private void PlayTextToSpeech(TextToSpeech textToSpeech)
    {
        ResetTextToSpeechState();
        line = textToSpeech;
        source.PlayOneShot(line.Speech);
        OnSpeechGenerated?.Invoke(this, new TextToSpeechEventArgs(line));
    }

    private void DispatchMessageTextToken(string token)
    {
        OnTextStreamTokenReceived?.Invoke(this, new TextEventArgs(token));
    }

    private string DispatchMessageText(string text)
    {
        OnMessage?.Invoke(this, new MessageEventArgs(text));
        return text;
    }

    private void OnGeneratedSpeechReceived(object sender, TextToSpeechEventArgs e)
    {
        lines.Enqueue(e.TextToSpeech);
        IsGeneratingSpeech = true;
    }

    private void OnGeneratedTextStreamReceived(object sender, GeneratedTextReceivedEventArgs<Choice.Chunk> e)
    {
        IsGeneratingText = true;
        var matches = false;
        var token = e.GeneratedText.Content ?? string.Empty;
        _buffer += token;
        foreach (var code in StopCodes)
            if (token.Contains(code))
                matches = true;
        if (!matches) return;
        if (wordMapping != null)
            wordMapping.Filter(_buffer);
        textToSpeech.Say(_buffer);
        _buffer = string.Empty;
    }

    private void OnGeneratedTextStreamEnded(object sender, GeneratedTextStreamEndedEventArgs e)
    {
        IsGeneratingText = false;
        OnTextGenerated?.Invoke(this, new TextEventArgs(e.Content));
    }

    private void OnGeneratedTextReceived(object sender, GeneratedTextReceivedEventArgs<Choice> e)
    {
        OnTextGenerated?.Invoke(this, new TextEventArgs(e.Content));
    }
}

public class MessageEventArgs : EventArgs
{
    public string Message { get; set; }
    
    public MessageEventArgs(string message)
    {
        Message = message;
    }
}