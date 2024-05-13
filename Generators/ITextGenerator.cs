using RSG;
using System;
using System.Collections.Generic;

public interface ITextGenerator
{
    public List<Message> Prompt { get; set; }
    public IPromise<string> RespondTo(string message);
    public void ResetContext();

    public event Func<string, IPromise<string>> OnTextGenerated;
    public event Action<string[]> OnContextReset;
}