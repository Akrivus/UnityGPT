using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class BooleanParamAttribute : ToolParamAttribute
{
    public BooleanParamAttribute(string name, string description, bool required = true, ToolParam items = null) : base(ParameterType.Boolean, name, description, required)
    {
        Items = items;
    }
}
