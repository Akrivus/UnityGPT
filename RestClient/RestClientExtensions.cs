using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Proyecto26;
using RSG;
using System;

public static class RestClientExtensions
{
    public static readonly JsonSerializerSettings DefaultSettings = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore,
        Converters = { new StringEnumConverter(typeof(SnakeCaseNamingStrategy)) },
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        },
    };

    public static IPromise<T> Post<T>(string uri, object @object)
    {
        return RestClient.Post(uri, Serialize(@object))
            .Then(response => response.Text)
            .Then(text => Deserialize<T>(text));
    }

    public static IPromise<T> Get<T>(string uri)
    {
        return RestClient.Get(uri)
            .Then(response => response.Text)
            .Then(text => Deserialize<T>(text));
    }

    public static T Deserialize<T>(string text)
    {
        return JsonConvert.DeserializeObject<T>(text, DefaultSettings);
    }

    public static object Deserialize(string text, Type type)
    {
        return JsonConvert.DeserializeObject(text, type, DefaultSettings);
    }

    public static string Serialize(object @object)
    {
        return JsonConvert.SerializeObject(@object, DefaultSettings);
    }
}
