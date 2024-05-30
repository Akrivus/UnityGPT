using RSG;

public abstract class BaseToolCaller<T, V> where T : IToolCall
{
    private static readonly string Prompt = "{0}\n\n{1}\nText:";

    private TextGenerator generator;

    protected TextGenerator AI => generator;

    public BaseToolCaller(LinkOpenAI client, IToolCall tool, string instruction, string prompt = "")
    {
        prompt = string.Format(Prompt, prompt, instruction);
        generator = new TextGenerator(client, prompt, "gpt-3.5-turbo", 256, 0.1f);
        generator.AddTool(tool);
    }

    public abstract IPromise<V> Call(string text, string context = "");

    protected IPromise<string> Execute(string text)
    {
        return generator.Execute<T>(text).Then(ReturnText);
    }

    private string ReturnText(string text)
    {
        generator.ResetContext();
        return text;
    }
}
