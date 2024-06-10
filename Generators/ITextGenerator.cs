using RSG;
using System;
using System.Collections.Generic;

public interface ITextGenerator
{
    public List<Message> Prompt { get; set; }
    public string LastMessage { get; }
    public IPromise<string> RespondTo(string message, params string[] context);
    public void AddContext(string context);
    public void AddMessage(string message);
    public void ResetContext();
    public ITextGenerator Fork(string prompt);

    public event Func<string, IPromise<string>> OnTextGenerated;
    public event Action<ITextGenerator> OnContextReset;
}