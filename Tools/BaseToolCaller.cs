using RSG;

public abstract class BaseToolCaller<T, V> where T : IToolCall
{
    private static readonly string Prompt = "{0}\n\n{1}\nText:";

    private TextGenerator generator;
    private IToolCall toolCall;

    private bool useToolResponse;

    protected TextGenerator AI => generator;

    public BaseToolCaller(LinkOpenAI client, IToolCall tool, bool useToolResponse, string instruction, string prompt = "")
    {
        this.useToolResponse = useToolResponse;
        prompt = Prompt.Format(prompt, instruction);
        toolCall = tool;
        generator = new TextGenerator(client, prompt, "gpt-3.5-turbo", 256, 0.1f);
        generator.AddTool(tool);
    }

    public abstract IPromise<V> Call(string text, string context = "");

    protected IPromise<string> Execute(string text)
    {
        return generator.Execute(toolCall.Tool.Name, text, useToolResponse);
    }
}
