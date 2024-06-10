using Proyecto26;
using RSG;
using System;
using UnityEngine;
using UnityEngine.Networking;

public class LinkOpenAI : MonoBehaviour
{
    public static string DefaultUri => "https://api.openai.com/v1/";
    public static string DefaultToken => Environment.GetEnvironmentVariable("OPENAI_API_KEY");

    public string Uri_Speech => apiEndpoint + "audio/speech";
    public string Uri_Transcriptions => apiEndpoint + "audio/transcriptions";
    public string Uri_Chat => apiEndpoint + "chat/completions";
    public string Uri_Embeddings => apiEndpoint + "embeddings";
    public string Uri_Login => apiEndpoint + "auth";

    [SerializeField]
    private string apiEndpoint;
    [SerializeField]
    private string accessToken;

    private void Awake()
    {
        if (Application.absoluteURL.IndexOf('?') > -1)
            accessToken = Application.absoluteURL.Split('?')[1].Split('=')[1];
        if (string.IsNullOrEmpty(accessToken))
            accessToken = DefaultToken;
        if (accessToken.StartsWith("sk-"))
            AutoLogin(accessToken);
        else
            Login(accessToken);
    }

    public void AutoLogin(string accessToken)
    {
        this.accessToken = accessToken;
    }

    public void Login(string secretToken)
        => Get<TokenResponse>(Uri_Login + "?token=" + secretToken)
            .Then((response) => accessToken = response.Token);

    public RequestHelper SetHeaders(RequestHelper helper, string contentType = null)
    {
        if (contentType != null)
            helper.Headers["Content-Type"] = contentType;
        helper.Headers["Authorization"] = "Bearer " + accessToken;
        helper.Headers["Accept"] = "*/*";
        return helper;
    }

    public IPromise<ResponseHelper> Post(RequestHelper helper, string contentType = null)
        => RestClient.Post(SetHeaders(helper, contentType));

    public IPromise<ResponseHelper> Post(string uri, string body, string contentType = null)
        => Post(new RequestHelper { Uri  = uri, BodyString = body }, contentType);

    public IPromise<T> Post<T>(string uri, object body)
        => Post(uri, RestClientExtensions.Serialize(body), "application/json")
            .Then(response => response.Text)
            .Then(text => RestClientExtensions.Deserialize<T>(text));

    public IPromise<ResponseHelper> Get(RequestHelper helper)
        => RestClient.Get(SetHeaders(helper));

    public IPromise<ResponseHelper> Get(string uri)
        => Get(new RequestHelper { Uri = uri });

    public IPromise<T> Get<T>(string uri)
        => Get(uri)
        .Then(response => response.Text)
        .Then(text => RestClientExtensions.Deserialize<T>(text));

    public IPromise<Texture2D> GetTexture(string uri) =>
        Get(new RequestHelper
        {
            Uri = uri,
            DownloadHandler = new DownloadHandlerTexture()
        }).Then((response) =>
        {
            var downloader = response.Request.downloadHandler as DownloadHandlerTexture;
            return downloader.texture;
        });

    public IPromise<AudioClip> GetAudioClip(string uri, AudioType audioType = AudioType.WAV) =>
        Get(new RequestHelper
        {
            Uri = uri,
            DownloadHandler = new DownloadHandlerAudioClip(uri, audioType)
        }).Then((response) =>
        {
            var downloader = response.Request.downloadHandler as DownloadHandlerAudioClip;
            return downloader.audioClip;
        });
}

public class TokenResponse
{
    public string Token { get; set; }

    public TokenResponse(string token)
    {
        Token = token;
    }

    public TokenResponse()
    {
    }
}