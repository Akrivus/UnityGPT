using RSG;

public interface ITextGenerator
{
    public string Context { get; set; }
    public IPromise<string> RespondTo(string context);
    public IPromise<string> SendContext();
    public void ResetContext();
    public void AddContext(string message);
}