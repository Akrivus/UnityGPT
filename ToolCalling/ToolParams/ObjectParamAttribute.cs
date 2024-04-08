using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ObjectParamAttribute : ToolParamAttribute
{
    public ObjectParamAttribute(string name, string description, bool required = true) : base(ParameterType.Object, name, description, required)
    {

    }
}
