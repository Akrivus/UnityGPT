using System;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class ToolAttribute : Attribute
{
    public string Name;
    public string Description;

    public ToolAttribute(string name, string description)
    {
        Name = name;
        Description = description;
    }
}
