using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[Serializable]
public class Tool
{
    public string Name;
    public string Description;
    public Params Parameters;

    public Tool(string name, string description, params ToolParam[] toolParams)
    {
        Name = name;
        Description = description;
        Parameters = new Params(toolParams);
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

}
