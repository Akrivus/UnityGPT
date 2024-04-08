using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ArrayParamAttribute : ToolParamAttribute
{
    public ArrayParamAttribute(string name, string description, bool required = true, ToolParam items = null) : base(ParameterType.Array, name, description, required)
    {
        Items = items;
    }
}
