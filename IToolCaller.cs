
public interface IToolCaller
{
    public void CallTool(ToolCallReference reference, IToolCall tool);
    public void AddTool(string name, IToolCall tool);
    public void AddTools(params IToolCall[] tools);
    public void RemoveTool(string name);
    public void RemoveTools(params string[] names);
}
