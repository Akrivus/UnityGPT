using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class NumberParamAttribute : ToolParamAttribute
{
    public NumberParamAttribute(string name, string description, bool required = true, float min = float.MinValue, float max = float.MaxValue) : base(ParameterType.Number, name, description, required)
    {
        Range = new Range(min, max);
    }

    public NumberParamAttribute(string name, string description, bool required = true, float min = float.MinValue, float max = float.MaxValue, float mutlipleOf = 1) : base(ParameterType.Number, name, description, required)
    {
        Range = new Range(min, max, mutlipleOf);
    }
}
