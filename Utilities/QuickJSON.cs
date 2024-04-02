using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;

public class QuickJSON
{
    static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore,
        Converters = { new StringEnumConverter(typeof(SnakeCaseNamingStrategy)) },
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        },
    };

    public static string Serialize(object @object)
    {
        return JsonConvert.SerializeObject(@object, Settings);
    }

    public static T Deserialize<T>(string @string)
    {
        return JsonConvert.DeserializeObject<T>(@string, Settings);
    }

    public static Dictionary<string, object> Deserialize(string @string)
    {
        return JsonConvert.DeserializeObject<Dictionary<string, object>>(@string, Settings);
    }

    public static object Deserialize(string @string, Type type)
    {
        return JsonConvert.DeserializeObject(@string, type, Settings);
    }
}