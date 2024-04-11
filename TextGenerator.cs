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
    public string ToolChoice { get; set; } = "auto";
    public string Prompt
    {
        get => prompt;
        set
        {
            prompt = value;
            AddSystemPrompt();
        }
    }

    public event EventHandler<GeneratedTextReceivedEventArgs<Choice>> OnGeneratedTextReceived;
    public event EventHandler<GeneratedTextReceivedEventArgs<Choice.Chunk>> OnGeneratedTextStreamReceived;
    public event EventHandler<GeneratedTextStreamEndedEventArgs> OnGeneratedTextStreamEnded;

    protected List<Tool> tools = new List<Tool>();
    protected List<Message> messages = new List<Message>();
    protected string prompt;
    protected string message;

    public TextGenerator(TextModel model, int maxTokens = 1024, float temperature = 0.5f)
    {
        Model = model;
        MaxTokens = maxTokens;
        Temperature = temperature;
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
        var body = new GenerateText(Model, MaxTokens, Temperature, messages, tools, ToolChoice);
        return RestClientExtensions.Post<GeneratedText<Choice>>(URI, body).Then(text => DispatchGeneratedText(text));
    }

    public IPromise<string> Listen(Action<string> tokenCallback)
    {
        var sse = new ServerSentEventHandler<GeneratedText<Choice.Chunk>>();
        sse.OnServerSentEvent += (e) => tokenCallback(DispatchGeneratedTextChunk(e.Data));

        message = string.Empty;

        var body = new GenerateText(Model, MaxTokens, Temperature, messages, tools, ToolChoice, true);
        return RestClient.Post(new RequestHelper
        {
            Uri = URI,
            BodyString = RestClientExtensions.Serialize(body),
            DownloadHandler = sse
        }).Then((helper) => DispatchGeneratedText(message));
    }

    public void ResetContext()
    {
        throw new NotImplementedException();
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

    public void AddSystemPrompt()
    {
        if (!string.IsNullOrEmpty(prompt) && messages.Count == 0)
            messages.Add(new Message(prompt, Roles.System));
    }

    private string DispatchGeneratedText(GeneratedText<Choice> text)
    {
        messages.Add(text.Choice.Message);
        OnGeneratedTextReceived?.Invoke(this, new GeneratedTextReceivedEventArgs<Choice>(text));
        return text.Content;
    }

    private string DispatchGeneratedTextChunk(GeneratedText<Choice.Chunk> text)
    {
        message += text.Content;
        OnGeneratedTextStreamReceived?.Invoke(this, new GeneratedTextReceivedEventArgs<Choice.Chunk>(text));
        return text.Content;
    }

    private string DispatchGeneratedText(string message)
    {
        OnGeneratedTextStreamEnded?.Invoke(this, new GeneratedTextStreamEndedEventArgs(message));
        return message;
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