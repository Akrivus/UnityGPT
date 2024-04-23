
public class ArrayParam : ToolParam
{
    public ArrayParam(string name, string description, bool required = true, ToolParam items = null) : base(ParameterType.Array, name, description, required)
    {
        Items = items;
    }
}
