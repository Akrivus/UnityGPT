using RSG;
using System.Collections.Generic;

public class ToolCaller : TextGenerator, IToolCaller
{
    public Dictionary<string, IToolCall> Tools = new Dictionary<string, IToolCall>();

    public ToolCaller(string prompt, TextModel model, int maxTokens = 1024, float temperature = 0.5f, params IToolCall[] tools) : base(prompt, model, maxTokens, temperature)
    {
        AddTool(tools);
    }

    public override void AddTool(params IToolCall[] tools)
    {
        foreach (var tool in tools)
            Tools.Add(tool.Tool.Name, tool);
        base.AddTool(tools);
    }

    public override void RemoveTool(params string[] names)
    {
        foreach (var name in names)
            Tools.Remove(name);
        base.RemoveTool(names);
    }

    public IPromise<string> Execute(string toolChoice, string input = "")
    {
        ToolChoice = toolChoice;
        return RespondTo(input);
    }

    private IPromise<string> ExecuteToolCalls(List<ToolCallReference> tools)
    {
        if (tools == null) return null;
        foreach (var tool in tools)
            ExecuteToolCall(tool, Tools[tool.Function.Name]);
        return SendContext();
    }

    private void ExecuteToolCall(ToolCallReference tool, IToolCall toolCall)
    {
        var parameters = RestClientExtensions.Deserialize(tool.Function.Arguments, toolCall.ArgType);
        var call = toolCall.EntryPoint.Invoke(toolCall, new object[] { parameters });
        var result = RestClientExtensions.Serialize(call);

        AddResult(tool, result);
    }

    protected override IPromise<string> DispatchForToolCalls<T>(GeneratedText<T> text)
    {
        if (text.ToolCalls != null)
            return ExecuteToolCalls(text.ToolCalls);
        return base.DispatchForToolCalls(text);
    }
}
