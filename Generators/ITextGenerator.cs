using RSG;
using System;

public interface ITextGenerator
{
    public string Context { get; set; }
    public IPromise<string> RespondTo(string context);
    public IPromise<string> SendContext();
    public void ResetContext();
    public void AddContext(string context);
    public void AddMessage(string message);

    public event Func<string, IPromise<string>> OnTextGenerated;
    public event Action<string[]> OnContextReset;
}