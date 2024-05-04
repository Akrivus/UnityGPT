using RSG;
using System;
using System.Collections.Generic;
using static Message;

public class TextGenerator : ITextGenerator, IToolCaller
{
    protected const string URI = "https://api.openai.com/v1/chat/completions";

    public event Func<string, IPromise<string>> OnTextGenerated;
    public event Action<string[]> OnContextReset;

    public TextModel Model { get; set; } = TextModel.GPT_3_Turbo;
    public int MaxTokens { get; set; } = 1024;
    public float Temperature { get; set; } = 0.5F;
    public string ToolChoice { get; set; } = null;
    public string Prompt { get; set; }

    protected List<Message> messages = new List<Message>();
    protected Dictionary<string, IToolCall> toolCalls = new Dictionary<string, IToolCall>();

    protected List<Tool> tools => GetToolList();

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

    public IPromise<string> RespondTo(string content)
    {
        AddContext(content);
        return SendContext();
    }

    public IPromise<string> SendContext()
    {
        var body = new GenerateText(Model, MaxTokens, Temperature, messages, tools, false, ToolChoice);
        return RestClientExtensions.Post<GeneratedText<Choice>>(URI, body)
            .Then(text => DispatchGeneratedText(text));
    }

    public void ResetContext()
    {
        var context = new string[messages.Count];
        for (int i = 0; i < messages.Count; i++)
            context[i] = messages[i].Content;
        OnContextReset?.Invoke(context);
        messages.Clear();
        messages.Add(new Message(Prompt, Roles.System));
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