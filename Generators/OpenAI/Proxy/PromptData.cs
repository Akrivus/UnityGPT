public class PromptData : IPhrenContext
{
    public string PromptId { get; set; }

    public PromptData(string promptId)
    {
        PromptId = promptId;
    }
}

public interface IPhrenContext
{
    public string PromptId { get; set; }
}