using RSG;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public class TextGenerator : ITextGenerator, IToolCaller
{
    public event Func<string, IPromise<string>> OnTextGenerated;
    public event Action<string[]> OnContextReset;

    public string Model { get; set; } = "gpt-3.5-turbo";
    public int MaxTokens { get; set; } = 1024;
    public float Temperature { get; set; } = 0.5F;
    public string ToolChoice { get; set; } = null;
    public string InterstitialPrompt { get; set; } = "{0}";
    public List<Message> Prompt { get; set; }

    protected List<Message> messages = new List<Message>();
    protected Dictionary<string, IToolCall> toolCalls = new Dictionary<string, IToolCall>();

    protected List<Tool> tools => GetToolList();

    protected LinkOpenAI api;

    public TextGenerator(LinkOpenAI api, List<Message> messages, string model, int maxTokens = 1024, float temperature = 0f, string interstitialPrompt = "{0}")
    {
        this.api = api;
        Prompt = messages;
        Model = model;
        MaxTokens = maxTokens;
        Temperature = temperature;
        InterstitialPrompt = interstitialPrompt;
        ResetContext();
    }

    public TextGenerator(LinkOpenAI client, string prompt, string model, int maxTokens = 1024, float temperature = 0f, string interstitialPrompt = "{0}")
        : this(client, new List<Message> { new Message(prompt, Roles.System) }, model, maxTokens, temperature, interstitialPrompt) { }

    public IPromise<string> RespondTo(string content, params string[] args)
    {
        content = string.Format(InterstitialPrompt, content, args);
        AddContext(content);
        return SendContext();
    }

    public IPromise<string> SendContext()
    {
        var body = new GenerateText(Model, MaxTokens, Temperature, messages, tools, false, ToolChoice);
        return api.Post<GeneratedText<Choice>>(api.Uri_Chat, body)
            .Then(text => DispatchGeneratedText(text));
    }

    public void ResetContext()
    {
        var context = new string[messages.Count];
        for (int i = 0; i < messages.Count; i++)
            context[i] = messages[i].Content;
        OnContextReset?.Invoke(context);
        messages.Clear();
        messages.AddRange(Prompt);
    }

    public void AddContext(string message)
    {
        messages.Add(new Message(message, Roles.User));
    }

    public void AddMessage(string message)
    {
        messages.Add(new Message(message, Roles.Assistant));
    }

    public IPromise<string> DispatchGeneratedText(GeneratedText<Choice> text)
    {
        messages.Add(text.Choice.Message);
        return ExecuteToolCalls(text.ToolCalls, text.Content)
            .Then((content) => OnTextGenerated?.Invoke(content)
                ?? Promise<string>.Resolved(content));
    }

    public void AddTool(params IToolCall[] toolCalls)
    {
        foreach (var toolCall in toolCalls)
            this.toolCalls.Add(toolCall.Tool.Name, toolCall);
    }

    public void RemoveTool(params string[] names)
    {
        foreach (var name in names)
            toolCalls.Remove(name);
    }

    public void AddResult(ToolCallReference tool, string result)
    {
        messages.Add(new Message(result, Roles.Tool,
            tool.Function.Name, tool.Id));
    }

    public IPromise<string> Execute<T>(string input = "") where T : IToolCall
    {
        foreach (var toolCall in toolCalls.Values)
            if (toolCall.GetType() == typeof(T))
                ToolChoice = toolCall.Tool.Name;
        return RespondTo(input);
    }

    protected IPromise<string> ExecuteToolCalls(List<ToolCallReference> tools, string response)
    {
        if (tools == null || tools.Count == 0)
            return Promise<string>.Resolved(response);
        foreach (var tool in tools)
            ExecuteToolCall(tool, toolCalls[tool.Function.Name]);
        ToolChoice = null;
        return SendContext();
    }

    private void ExecuteToolCall(ToolCallReference tool, IToolCall toolCall)
    {
        var parameters = RestClientExtensions.Deserialize(tool.Function.Arguments, toolCall.ArgType);
        var call = toolCall.Execute(parameters);
        var result = RestClientExtensions.Serialize(call);

        AddResult(tool, result);
    }

    private List<Tool> GetToolList()
    {
        var tools = new List<Tool>();
        foreach (var toolCall in toolCalls.Values)
            tools.Add(toolCall.Tool);
        return tools;
    }
}