using RSG;

public interface IToolCaller
{
    public bool UseToolResponse { get; set; }

    public void AddTool(params IToolCall[] tools);
    public void RemoveTool(params string[] names);
    public IPromise<string> Execute(string function, string input = "", bool useToolResponse = true);
}
