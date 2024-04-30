using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

public enum ParameterType
{
    String, Number, Integer, Object, Array, Boolean, Null
}

public class ToolParam
{
    [SerializeField]
    public ParameterType Type;
    [SerializeField]
    public string Name;
    [SerializeField]
    public string Description;

    [JsonIgnore]
    public bool IsRequired;

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public int? MinLength;
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public int? MaxLength;
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string Pattern;
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string[] Enum;
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string Format;
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public float? Minimum;
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public float? Maximum;
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public float? MultipleOf;
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public ToolParam Items;
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, ToolParam> Properties;
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [SerializeField]
    public string[] Required;

    [JsonIgnore]
    public List<ToolParam> Definitions;

    public ToolParam() { }

    public ToolParam(ParameterType type, string name, string description, bool required, params ToolParam[] definitions)
    {
        Type = type;
        Name = name;
        Description = description;
        IsRequired = required;
        Definitions = new List<ToolParam>(definitions);
        Properties = GenerateProperties();
        Required = GenerateRequiredFields();
    }

    private Dictionary<string, ToolParam> GenerateProperties()
    {
        var properties = new Dictionary<string, ToolParam>();
        foreach (var definition in Definitions)
            properties[definition.Name] = definition;
        return properties;
    }

    private string[] GenerateRequiredFields()
    {
        var required = new List<string>();
        foreach (var definition in Definitions)
            if (definition.IsRequired)
                required.Add(definition.Name);
        return required.ToArray();
    }
}
