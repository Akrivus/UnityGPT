using RSG;
using System.Collections.Generic;
using System.Diagnostics;
using static Message;

public class TextAgent : TextGenerator, IToolCaller
{
    public Dictionary<string, IToolCall> Tools = new Dictionary<string, IToolCall>();

    public TextAgent(TextModel model, int maxTokens = 1024, float temperature = 0.5f, params IToolCall[] tools) : base(model, maxTokens, temperature)
    {
        OnGeneratedTextReceived += (sender, e) => ExecuteToolCalls(e.GeneratedText.ToolCalls);
        OnGeneratedTextStreamReceived += (sender, e) => ExecuteToolCalls(e.GeneratedText.ToolCalls);
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
        return Ask(input);
    }

    private IPromise<string> ExecuteToolCalls(List<ToolCallReference> tools)
    {
        if (tools == null) return null;
        foreach (var tool in tools)
            ExecuteToolCall(tool, Tools[tool.Function.Name]);
        return Listen();
    }

    private void ExecuteToolCall(ToolCallReference tool, IToolCall toolCall)
    {
        var parameters = RestClientExtensions.Deserialize(tool.Function.Arguments, toolCall.ArgType);
        var call = toolCall.EntryPoint.Invoke(toolCall, new object[] { parameters });
        var result = RestClientExtensions.Serialize(call);

        AddResult(tool, result);
    }
}
