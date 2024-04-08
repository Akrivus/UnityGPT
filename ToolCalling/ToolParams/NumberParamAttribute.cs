using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class NumberParamAttribute : ToolParamAttribute
{
    public NumberParamAttribute(string name, string description, bool required = true, float min = float.MinValue, float max = float.MaxValue, float? multipleOf = null) : base(ParameterType.Number, name, description, required)
    {
        Range = new Range(min, max, multipleOf);
    }
}
