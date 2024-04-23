
public class StringParam : ToolParam
{
    public StringParam(string name, string description, bool required = true, int minLength = 0, int maxLength = int.MaxValue)
        : base(ParameterType.String, name, description, required)
    {
        MinLength = minLength;
        MaxLength = maxLength;
    }

    public StringParam(string name, string description, bool required, string pattern)
        : base(ParameterType.String, name, description, required)
    {
        Pattern = pattern;
    }

    public StringParam(string name, string description, bool required, params string[] enums)
        : base(ParameterType.String, name, description, required)
    {
        Enum = enums;
    }
}
