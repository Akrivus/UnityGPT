using System;
using System.Collections.Generic;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ToolParamAttribute : Attribute
{
    public ParameterType Type;
    public string Name;
    public string Description;
    public bool Required;
    public string Pattern;
    public string[] Enum;
    public string Format;
    public Length Length;
    public Range Range;
    public ToolParam Items;
    public Dictionary<string, ToolParam> Properties;

    public ToolParamAttribute(ParameterType type, string name, string description,
        bool required = true)
    {
        Type = type;
        Name = name;
        Description = description;
        Required = required;
    }
}

public struct Length
{
    public int Min;
    public int Max;

    public Length(int min, int max)
    {
        Min = min;
        Max = max;
    }
}

public struct Range
{
    public float Min;
    public float Max;
    public float? MultipleOf;

    public Range(float min, float max, float? multipleOf = null)
    {
        Min = min;
        Max = max;
        MultipleOf = multipleOf;
    }
}