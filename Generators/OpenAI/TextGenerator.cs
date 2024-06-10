using RSG;
using System;
using System.Collections.Generic;

public class TextGenerator : ITextGenerator, IToolCaller
{
    public event Func<string, IPromise<string>> OnTextGenerated;
    public event Action<ITextGenerator> OnContextReset;

    public string Model { get; set; } = "gpt-3.5-turbo";
    public int MaxTokens { get; set; } = 1024;
    public float Temperature { get; set; } = 0.5F;
    public string ToolChoice { get; set; } = null;
    public string InterstitialPrompt { get; set; } = "{0}";
    public List<Message> Prompt { get; set; }
    public bool UseToolResponse { get; set; } = true;

    public string LastMessage => messages[Math.Max(messages.Count - 1, 0)].Content;

    protected List<Message> messages = new List<Message>();
    protected Dictionary<string, IToolCall> toolCalls = new Dictionary<string, IToolCall>();

    protected List<Tool> tools => GetToolList();

    protected LinkOpenAI client;

    public TextGenerator(LinkOpenAI api, List<Message> messages, string model, int maxTokens = 1024, float temperature = 0f, string interstitialPrompt = "{0}")
    {
        this.client = api;
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
        content = InterstitialPrompt.Format(content, args);
        AddContext(content);
        return SendMessages();
    }

    public IPromise<string> SendMessagesForToolCalls()
    {
        if (UseToolResponse)
            return SendMessages();
        ResetContext();
        UseToolResponse = true;
        return Promise<string>.Resolved("OK");
    }

    public IPromise<string> SendMessages()
    {
        var body = new GenerateText(Model, MaxTokens, Temperature, messages, tools, false, ToolChoice);
        return client.Post<GeneratedText<Choice>>(client.Uri_Chat, body)
            .Then(text => DispatchGeneratedText(text));
    }

    public ITextGenerator Fork(string prompt)
    {
        return new TextGenerator(client, prompt, Model, MaxTokens, Temperature, InterstitialPrompt);
    }

    public void ResetContext()
    {
        OnContextReset?.Invoke(this);
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

    public void AddToolCalls(ToolCallReference[] toolCalls)
    {
        messages.Add(new Message(toolCalls));
    }

    public IPromise<string> DispatchGeneratedText(GeneratedText<Choice> text)
    {
        if (text.ToolCall) return ExecuteToolCalls(text.ToolCalls).Then((_) => SendMessagesForToolCalls());

        messages.Add(text.Choice.Message);

        OnTextGenerated?.Invoke(text.Content);
        return Promise<string>.Resolved(text.Content);
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

    public string AddResult(ToolCallReference toolCall, string result)
    {
        messages.Add(new Message(result, toolCall.Id));
        return result;
    }

    public IPromise<string> Execute(string function, string input = "", bool useToolResponse = true)
    {
        foreach (var toolCall in toolCalls.Values)
            if (toolCall.Tool.Name == function)
                ToolChoice = toolCall.Tool.Name;
        UseToolResponse = useToolResponse;
        return RespondTo(input);
    }

    protected IPromise<string> ExecuteToolCalls(ToolCallReference[] toolCalls)
    {
        AddToolCalls(toolCalls);

        var promise = ExecuteToolCall(toolCalls[0]);
        for (var i = 1; i < toolCalls.Length; i++)
            promise = promise.Then((_) => ExecuteToolCall(toolCalls[i]));

        ToolChoice = null;
        return promise;
    }

    private IPromise<string> ExecuteToolCall(ToolCallReference toolCall)
    {
        if (toolCall.Function.Name == null || !toolCalls.ContainsKey(toolCall.Function.Name))
            return Promise<string>.Resolved("Function not found.");
        var tool = toolCalls[toolCall.Function.Name];
        var args = RestClientExtensions.Deserialize(toolCall.Function.Arguments, tool.ArgType);
        return tool.Execute(args)
            .Then((call) => RestClientExtensions.Serialize(call))
            .Then(result => AddResult(toolCall, result));
    }

    private List<Tool> GetToolList()
    {
        var tools = new List<Tool>();
        foreach (var toolCall in toolCalls.Values)
            tools.Add(toolCall.Tool);
        return tools;
    }
}