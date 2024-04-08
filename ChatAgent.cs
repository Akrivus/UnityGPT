using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ChatAgent : MonoBehaviour, IText
{
    public readonly static RestClient API = new RestClient("https://api.openai.com/v1", Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
    readonly static string[] StopCodes = new string[] { ".", "?", "!", "\n" };

    public event EventHandler<TextEventArgs> TextStart;
    public event EventHandler<TextEventArgs> TextUpdate;
    public event EventHandler<TextEventArgs> TextComplete;

    public event EventHandler<TextToSpeechEventArgs> TextToSpeechStart;
    public event EventHandler<TextToSpeechEventArgs> TextToSpeechUpdate;
    public event EventHandler<TextToSpeechEventArgs> TextToSpeechComplete;

    public event EventHandler<ChatEventArgs> ChatStart;
    public event EventHandler<ChatEventArgs> ChatUpdate;
	public event EventHandler<ChatEventArgs> ChatComplete;

    [TextArea(3, 10)]
    [SerializeField] string prompt = "You are a helpful assistant inside of a Unity scene.";
    [SerializeField] TextModel model = TextModel.GPT35_Turbo;
    [SerializeField] TextToSpeechModel voiceModel = TextToSpeechModel.TTS_1;
    [SerializeField] int maxTokens = 4096;
    [SerializeField] float temperature = 1.0f;
    [SerializeField] GenerateTextToSpeech.Voices voice = GenerateTextToSpeech.Voices.Onyx;
    [SerializeField] AudioSource source;
    [SerializeField, Range(0.8f, 1.2f)] float pitch = 1.0f;
    [SerializeField] WordMapping wordMapping;

    TextGenerator text;
    TextToSpeechGenerator textToSpeech;

    Queue<TextToSpeech> lines = new Queue<TextToSpeech>();

    string message;
    string messageBuffer;
    TextToSpeech messageTTS;
    bool _isTexting;
    bool _isTalking;
    bool _isComplete;

    public IEmbedding Embeddings => text;
    public ITextToSpeech TextToSpeech => textToSpeech;

    public string SystemPrompt
    {
        get => prompt;
        set
        {
            prompt = text.Prompt = value;
            text.ClearMessages();
        }
    }

    public bool IsTexting
    {
        get => _isTexting;
        private set
        {
            _isTexting = value;
            (value ? TextStart : TextComplete)?.Invoke(this, new TextEventArgs(message));
        }
    }
    public bool IsTalking
    {
        get => _isTalking;
        private set
        {
            _isTalking = value;
            (value ? TextToSpeechStart : TextToSpeechComplete)?.Invoke(this, new TextToSpeechEventArgs(message));
        }
    }
    public bool IsComplete
    {
        get => _isComplete;
        private set
        {
            _isComplete = value;
            (value ? ChatComplete : ChatStart)?.Invoke(this, new ChatEventArgs(message));
        }
    }

    public bool IsExited { get; set; }

    public bool IsSpeaking => lines.Count > 0 || source.isPlaying;
    public bool IsGenerating => IsTexting || IsTalking;

    void Awake()
    {
        text = new TextGenerator(prompt, model, maxTokens, temperature);
        text.TextUpdate += OnTextUpdate;
        text.TextComplete += OnTextComplete;
        text.AddTools(GetComponents<IToolCall>());
        textToSpeech = new TextToSpeechGenerator(voiceModel, voice);
        textToSpeech.TextToSpeechStart += OnTextToSpeechStart;

        if (!string.IsNullOrEmpty(message))
            StartCoroutine(GenerateText(message));
        if (wordMapping == null)
            wordMapping = ScriptableObject.CreateInstance<WordMapping>();
    }

    void Update()
    {
        if (!IsGenerating && !IsComplete)
            IsComplete = true;
		if (IsTalking && !IsSpeaking)
			IsTalking = false;
        if (lines.TryPeek(out var tts))
        {
            if (!tts.IsReady || source.isPlaying)
                return;
            TextToSpeechUpdate?.Invoke(this, new TextToSpeechEventArgs(tts.Text));
            source.pitch = pitch;
            source.clip = tts.Speech;
            source.Play();
            lines.Dequeue();
        }
    }

    void OnTextUpdate(object sender, TextEventArgs e)
    {
        var content = e.Message ?? string.Empty;
        messageBuffer += content;
        var matches = false;
        foreach (var code in StopCodes)
            if (content.Contains(code))
                matches = true;
        if (!matches)
            return;
        message += messageBuffer;
        messageTTS = new TextToSpeech(wordMapping.Filter(messageBuffer));
        lines.Enqueue(messageTTS);
        textToSpeech.GenerateSpeech(messageTTS);
        TextUpdate?.Invoke(this, new TextEventArgs(messageBuffer));
        messageBuffer = string.Empty;
    }

    void OnTextComplete(object sender, TextEventArgs e)
    {
        IsTexting = false;
    }

    void OnTextToSpeechStart(object sender, TextToSpeechEventArgs e)
    {
        IsTalking = true;
        ChatUpdate?.Invoke(this, new ChatEventArgs(e.Text));
    }

    public Task<string> GenerateTextAsync(string content)
    {
        message = messageBuffer = string.Empty;
        IsTexting = true;
        IsComplete = false;
        return text.GenerateTextAsync(content);
    }

    public IEnumerator GenerateText(string content)
    {
        message = messageBuffer = string.Empty;
        IsTexting = true;
        IsComplete = false;
        yield return text.GenerateText(content);
    }

    public void ClearMessages()
    {
        message = messageBuffer = string.Empty;
        text.ClearMessages();
    }
}

public class ChatEventArgs : EventArgs
{
    public string Message { get; set; }

    public ChatEventArgs(string message)
    {
        Message = message;
    }
}