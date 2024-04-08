using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ChatGenerator : MonoBehaviour, IText
{
    public readonly static RestClient API = new RestClient("https://api.openai.com/v1", Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
    readonly static string[] StopCodes = new string[] { ".", "?", "!", "\n" };

    public event EventHandler<TextEvent> TextStart;
    public event EventHandler<TextEvent> TextUpdate;
    public event EventHandler<TextEvent> TextComplete;

    public event EventHandler<TextToSpeechEvent> TextToSpeechStart;
    public event EventHandler<TextToSpeechEvent> TextToSpeechUpdate;
    public event EventHandler<TextToSpeechEvent> TextToSpeechComplete;

    public event EventHandler<ChatEvent> ChatStart;
    public event EventHandler<ChatEvent> ChatUpdate;
	public event EventHandler<ChatEvent> ChatComplete;

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
            (value ? TextStart : TextComplete)?.Invoke(this, new TextEvent(message));
        }
    }
    public bool IsTalking
    {
        get => _isTalking;
        private set
        {
            _isTalking = value;
            (value ? TextToSpeechStart : TextToSpeechComplete)?.Invoke(this, new TextToSpeechEvent(message));
        }
    }
    public bool IsComplete
    {
        get => _isComplete;
        private set
        {
            _isComplete = value;
            (value ? ChatComplete : ChatStart)?.Invoke(this, new ChatEvent(message));
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
            TextToSpeechUpdate?.Invoke(this, new TextToSpeechEvent(tts.Text));
            source.pitch = pitch;
            source.clip = tts.Speech;
            source.Play();
            lines.Dequeue();
        }
    }

    void OnTextUpdate(object sender, TextEvent e)
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
        TextUpdate?.Invoke(this, new TextEvent(messageBuffer));
        messageBuffer = string.Empty;
    }

    void OnTextComplete(object sender, TextEvent e)
    {
        IsTexting = false;
    }

    void OnTextToSpeechStart(object sender, TextToSpeechEvent e)
    {
        IsTalking = true;
        ChatUpdate?.Invoke(this, new ChatEvent(e.Text));
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

public class ChatEvent : EventArgs
{
    public string Message { get; set; }

    public ChatEvent(string message)
    {
        Message = message;
    }
}