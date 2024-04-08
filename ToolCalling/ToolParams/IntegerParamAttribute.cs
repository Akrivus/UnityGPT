using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class IntegerParamAttribute : ToolParamAttribute
{
    public IntegerParamAttribute(string name, string description, bool required = true, int min = int.MinValue, int max = int.MaxValue, int? multipleOf = null) : base(ParameterType.Integer, name, description, required)
    {
        Range = new Range(min, max, multipleOf);
    }
}
