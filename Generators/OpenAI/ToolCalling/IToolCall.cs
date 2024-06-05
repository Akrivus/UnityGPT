using RSG;
using System;

public interface IToolCall
{
    public Type ArgType { get; }
    public Tool Tool { get; }
    public IPromise<string> Execute(object args);
}
