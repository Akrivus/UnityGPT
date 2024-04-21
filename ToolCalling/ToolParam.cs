using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public enum ParameterType
{
    String, Number, Integer, Object, Array, Boolean, Null
}

public class ToolParam
{
    public ParameterType Type;
    public string Name;
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

    public ToolParam(ToolParamAttribute attr) : this(attr.Type, attr.Name, attr.Description, attr.Required)
    {
        MinLength = attr.Length.Min;
        MaxLength = attr.Length.Max;
        Pattern = attr.Pattern;
        Enum = attr.Enum;
        Format = attr.Format;
        Minimum = attr.Range.Min;
        Maximum = attr.Range.Max;
        MultipleOf = attr.Range.MultipleOf;
        Items = attr.Items;
        Properties = attr.Properties;
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

public class Params
{
    [HideInInspector]
    public readonly ParameterType Type = ParameterType.Object;
    public Dictionary<string, ToolParam> Properties;
    public string[] Required;

    [JsonIgnore]
    public ToolParam[] Definitions;

    public Params() { }

    public Params(params ToolParam[] definitions)
    {
        Definitions = definitions;
    }

    public Params(Type type)
    {
        Definitions = GenerateDefinitions(type);
        Properties = GenerateProperties();
        Required = GenerateRequiredFields();
    }

    private ToolParam[] GenerateDefinitions(Type type)
    {
        var definitions = new List<ToolParam>();
        var fields = type.GetFields();
        foreach (var field in fields)
        {
            var _ = field.GetCustomAttribute<ToolParamAttribute>();
            if (_ != null)
                definitions.Add(new ToolParam(_));
        }
        return definitions.ToArray();
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
