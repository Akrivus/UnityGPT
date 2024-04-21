using System;
using System.Reflection;
using UnityEngine;

[Serializable]
public class Tool
{
    public string Name;
    public string Description;
    public Params Parameters;

    public Tool(Type type)
    {
        var attr = type.GetCustomAttribute<ToolAttribute>();
        Name = attr.Name;
        Description = attr.Description;
        Parameters = new Params(type);
    }
}

public interface IToolCall
{
    public Tool Tool { get; }
    public MethodInfo EntryPoint { get; }
    public Type ArgType { get; }
}
