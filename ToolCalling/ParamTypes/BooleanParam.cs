
public class BooleanParam : ToolParam
{
    public BooleanParam(string name, string description, bool required = true, ToolParam items = null) : base(ParameterType.Boolean, name, description, required)
    {
        Items = items;
    }
}
