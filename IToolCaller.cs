
using RSG;

public interface IToolCaller
{
    public void AddTool(params IToolCall[] tools);
    public void RemoveTool(params string[] names);
    public IPromise<string> Execute(string toolChoice, string input);
}
