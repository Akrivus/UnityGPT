using RSG;
using System;

public interface ITextGenerator
{
    public string Prompt { get; set; }
    public IPromise<string> RespondTo(string message);
    public void ResetContext();

    public event Func<string, IPromise<string>> OnTextGenerated;
    public event Action<string[]> OnContextReset;
}