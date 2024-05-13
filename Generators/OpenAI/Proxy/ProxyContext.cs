public class ProxyContext : IProxyContext
{
    public string PromptId { get; set; }

    public ProxyContext(string promptId)
    {
        PromptId = promptId;
    }
}

public interface IProxyContext
{
    public string PromptId { get; set; }
}