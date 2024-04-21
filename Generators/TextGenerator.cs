using RSG;
using System;
using System.Collections.Generic;
using static Message;

public class TextGenerator : ITextGenerator
{
    protected const string URI = "https://api.openai.com/v1/chat/completions";

    public event Action<GeneratedText<Choice>> OnGeneratedTextReceived;

    public TextModel Model { get; set; } = TextModel.GPT_3p5_Turbo;
    public int MaxTokens { get; set; } = 1024;
    public float Temperature { get; set; } = 0.5F;
    public string ToolChoice { get; set; } = null;
    public string Context { get; set; }

    protected List<Tool> tools = new List<Tool>();
    protected List<Message> messages = new List<Message>();

    public TextGenerator(TextModel model, int maxTokens = 1024, float temperature = 0.5f)
    {
        Model = model;
        MaxTokens = maxTokens;
        Temperature = temperature;
    }

    public TextGenerator(string prompt, TextModel model, int maxTokens = 1024, float temperature = 0.5f) : this(model, maxTokens, temperature)
    {
        Context = prompt;
        ResetContext();
    }

    public IPromise<string> RespondTo(string content)
    {
        messages.Add(new Message(content, Roles.User));
        return SendContext();
    }

    public IPromise<string> SendContext()
    {
        var body = new GenerateText(Model, MaxTokens, Temperature, messages, tools, false, ToolChoice);
        return RestClientExtensions.Post<GeneratedText<Choice>>(URI, body).Then(text => DispatchGeneratedText(text));
    }

    public void ResetContext()
    {
        messages.Clear();
        messages.Add(new Message(Context, Roles.System));
    }

    public void AddContext(string message)
    {
        messages.Add(new Message(message, Roles.User));
    }

    private IPromise<string> DispatchGeneratedText(GeneratedText<Choice> text)
    {
        messages.Add(text.Choice.Message);
        OnGeneratedTextReceived?.Invoke(text);
        return DispatchForToolCalls(text).Then(result => result);
    }

    protected virtual IPromise<string> DispatchForToolCalls<T>(GeneratedText<T> text) where T : Choice
    {
        return Promise<string>.Resolved(text.Content);
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
}