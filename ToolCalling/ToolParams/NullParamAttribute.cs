using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class NullParamAttribute : ToolParamAttribute
{
    public NullParamAttribute(string name, string description, bool required = true) : base(ParameterType.Null, name, description, required)
    {

    }
}
