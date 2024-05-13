using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Proyecto26;
using RSG;
using System;
using UnityEngine.Networking;
using UnityEngine;

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

    public static IPromise<T> Post<T>(string uri, object @object = null)
        => RestClient.Post(uri, Serialize(@object)).Then(response => response.Text).Then(text => Deserialize<T>(text));

    public static IPromise<T> Get<T>(string uri)
        => RestClient.Get(uri).Then(response => response.Text).Then(text => Deserialize<T>(text));

    public static T Deserialize<T>(string text)
        => JsonConvert.DeserializeObject<T>(text, DefaultSettings);

    public static object Deserialize(string text, Type type)
        => JsonConvert.DeserializeObject(text, type, DefaultSettings);

    public static string Serialize(object @object)
        => JsonConvert.SerializeObject(@object, DefaultSettings);

    public static IPromise<Texture2D> GetTexture(string uri) =>
        RestClient.Get(new RequestHelper
        {
            Uri = uri,
            DownloadHandler = new DownloadHandlerTexture()
        }).Then((response) =>
        {
            var downloader = response.Request.downloadHandler as DownloadHandlerTexture;
            return downloader.texture;
        });

    public static IPromise<AudioClip> GetAudioClip(string uri, AudioType audioType = AudioType.WAV) =>
        RestClient.Get(new RequestHelper
        {
            Uri = uri,
            DownloadHandler = new DownloadHandlerAudioClip(uri, audioType)
        }).Then((response) =>
        {
            var downloader = response.Request.downloadHandler as DownloadHandlerAudioClip;
            return downloader.audioClip;
        });
}
