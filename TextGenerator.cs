using Proyecto26;
using RSG;
using System;
using System.Collections.Generic;

using static Message;

public class TextGenerator : IText
{
    const string URI = "https://api.openai.com/v1/chat/completions";

    public TextModel Model { get; set; } = TextModel.GPT35_Turbo;
    public int MaxTokens { get; set; } = 1024;
    public float Temperature { get; set; } = 0.5F;
    public string ToolChoice { get; set; } = null;
    public string Prompt { get; set; }

    public event EventHandler<GeneratedTextReceivedEventArgs<Choice>> OnGeneratedTextReceived;
    public event EventHandler<GeneratedTextReceivedEventArgs<Choice.Chunk>> OnGeneratedTextStreamReceived;
    public event EventHandler<GeneratedTextStreamEndedEventArgs> OnGeneratedTextStreamEnded;

    protected List<Tool> tools = new List<Tool>();
    protected List<Message> messages = new List<Message>();
    protected string message;

    public TextGenerator(TextModel model, int maxTokens = 1024, float temperature = 0.5f)
    {
        Model = model;
        MaxTokens = maxTokens;
        Temperature = temperature;
    }

    public TextGenerator(string prompt, TextModel model, int maxTokens = 1024, float temperature = 0.5f) : this(model, maxTokens, temperature)
    {
        Prompt = prompt;
        ResetContext();
    }

    public void Tell(string content)
    {
        messages.Add(new Message(content, Roles.User));
    }

    public IPromise<string> Ask(string content)
    {
        Tell(content);
        return Listen();
    }

    public IPromise<string> Ask(string content, Action<string> tokenCallback)
    {
        Tell(content);
        return Listen(tokenCallback);
    }

    public IPromise<string> Listen()
    {
        var body = new GenerateText(Model, MaxTokens, Temperature, messages, tools, false, ToolChoice);
        return RestClientExtensions.Post<GeneratedText<Choice>>(URI, body).Then(text => DispatchGeneratedText(text));
    }

    public IPromise<string> Listen(Action<string> tokenCallback)
    {
        var sse = new ServerSentEventHandler<GeneratedText<Choice.Chunk>>();
        sse.OnServerSentEvent += (e) => DispatchGeneratedTextChunk(e.Data).Then(token => tokenCallback(token));

        message = string.Empty;

        var body = new GenerateText(Model, MaxTokens, Temperature, messages, tools, true, ToolChoice);
        return RestClient.Post(new RequestHelper
        {
            Uri = URI,
            BodyString = RestClientExtensions.Serialize(body),
            DownloadHandler = sse
        }).Then(_ => DispatchGeneratedText(message));
    }

    public void ResetContext()
    {
        messages.Clear();
        messages.Add(new Message(Prompt, Roles.System));
    }

    public virtual void AddTool(params IToolCall[] toolCalls)
    {
        foreach (var toolCall in toolCalls)
            tools.Add(toolCall.Tool);
    }

    public virtual void RemoveTool(params string[] names)
    {
        foreach (var name in names)
            tools.RemoveAll(tool => tool.Name == name);
    }

    public virtual void AddResult(ToolCallReference tool, string result)
    {
        messages.Add(new Message(result, Roles.Tool, tool.Function.Name, tool.Id));
    }

    private IPromise<string> DispatchGeneratedText(GeneratedText<Choice> text)
    {
        messages.Add(text.Choice.Message);
        OnGeneratedTextReceived?.Invoke(this, new GeneratedTextReceivedEventArgs<Choice>(text));
        return DispatchForToolCalls(text).Then(result => result);
    }

    private IPromise<string> DispatchGeneratedTextChunk(GeneratedText<Choice.Chunk> text)
    {
        if (text.Content != null) message += text.Content;
        OnGeneratedTextStreamReceived?.Invoke(this, new GeneratedTextReceivedEventArgs<Choice.Chunk>(text));
        return DispatchForToolCalls(text).Then(result => result);
    }

    private string DispatchGeneratedText(string message)
    {
        OnGeneratedTextStreamEnded?.Invoke(this, new GeneratedTextStreamEndedEventArgs(message));
        return message;
    }

    protected virtual IPromise<string> DispatchForToolCalls<T>(GeneratedText<T> text) where T : Choice
    {
        return Promise<string>.Resolved(text.Content);
    }
}

public class GeneratedTextReceivedEventArgs<T> : EventArgs where T : Choice
{
    public GeneratedText<T> GeneratedText { get; private set; }
    public string Content => GeneratedText.Content;

    public GeneratedTextReceivedEventArgs(GeneratedText<T> text)
    {
        GeneratedText = text;
    }
}

public class GeneratedTextStreamEndedEventArgs : EventArgs
{
    public string Content { get; private set; }

    public GeneratedTextStreamEndedEventArgs(string content)
    {
        Content = content;
    }
}