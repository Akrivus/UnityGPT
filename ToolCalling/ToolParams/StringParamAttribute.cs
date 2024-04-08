using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class StringParamAttribute : ToolParamAttribute
{
    public StringParamAttribute(string name, string description, bool required = true, int minLength = 0, int maxLength = int.MaxValue) : base(ParameterType.String, name, description, required)
    {
        Length = new Length(minLength, maxLength);
    }

    public StringParamAttribute(string name, string description, bool required, string pattern) : base(ParameterType.String, name, description, required)
    {
        Pattern = pattern;
    }

    public StringParamAttribute(string name, string description, bool required, params string[] enums) : base(ParameterType.String, name, description, required)
    {
        Enum = enums;
    }
}
