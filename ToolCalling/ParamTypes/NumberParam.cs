
public class NumberParam : ToolParam
{
    public NumberParam(string name, string description, bool required = true, float min = float.MinValue, float max = float.MaxValue, float? mutlipleOf = null)
        : base(ParameterType.Number, name, description, required)
    {
        Minimum = min;
        Maximum = max;
        MultipleOf = mutlipleOf;
    }
}
