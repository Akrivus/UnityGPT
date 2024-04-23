
public class IntegerParam : ToolParam
{
    public IntegerParam(string name, string description, bool required = true, int min = int.MinValue, int max = int.MaxValue, int? multipleOf = null)
        : base(ParameterType.Integer, name, description, required)
    {
        Minimum = min;
        Maximum = max;
        MultipleOf = multipleOf;
    }
}
