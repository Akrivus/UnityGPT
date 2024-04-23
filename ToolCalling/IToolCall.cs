using System;

public interface IToolCall
{
    public Type ArgType { get; }
    public Tool Tool { get; }
    public string Execute(object args);
}
