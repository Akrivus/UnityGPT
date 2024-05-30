using Proyecto26;
using RSG;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PhrenProxyClient : MonoBehaviour
{
    public static string DefaultUri => "https://api.openai.com/v1/";
    public static string DefaultToken => Environment.GetEnvironmentVariable("OPENAI_API_KEY");

    public event Action<ProxySession> OnSuccessfulLink;

    public string Uri_Speech => endpoint + "audio/speech";
    public string Uri_Transcriptions => endpoint + "audio/transcriptions";
    public string Uri_Chat => endpoint + "chat/completions";
    public string Uri_Embeddings => endpoint + "embeddings";
    public string Uri_Login => endpoint + "api";

    [SerializeField]
    private string endpoint;
    [SerializeField]
    private string promptId;
    [SerializeField]
    private ProxySessionFactory defaults;
    [SerializeField]
    private bool isAutoLogin = false;

    private string accessToken;

    private void Awake()
    {
        if (isAutoLogin) AutoLogin(defaults.Build());
    }

    public IPromise<ProxySession> Login<T>(T context) where T : IProxyContext
        => Post<ProxySession>(Uri_Login, context)
        .Then((session) => Promise<ProxySession>.Resolved(AutoLogin(session)));

    public IPromise<ProxySession> Login()
        => isAutoLogin ? AutoLogin()
        : Login(new ProxyContext(promptId));

    public IPromise<ProxySession> AutoLogin()
    {
        accessToken = DefaultToken;
        endpoint = DefaultUri;

        var session = defaults.Build(DefaultUri, accessToken);
        OnSuccessfulLink?.Invoke(session);
        return Promise<ProxySession>.Resolved(session);
    }

    public ProxySession AutoLogin(ProxySession session)
    {
        accessToken = session.AccessToken;
        OnSuccessfulLink?.Invoke(session);
        return session;
    }

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

    [Serializable]
    private struct ProxySessionFactory
    {
        [TextArea(3, 9)]
        public string Prompt;
        [Range(0, 1)]
        public float Temperature;
        public string Name;
        public string Description;
        public string Metadata;
        public string Model;
        public string Voice;
        public int MaxTokens;
        [TextArea(2, 8)]
        public string InterstitialPrompt;
            
        public List<Message> Messages => new List<Message> { new Message(Prompt, Roles.System) };

        public ProxySession Build(string uri, string token)
        {
            return new ProxySession
            {
                Href = uri,
                AccessToken = token,
                Name = Name,
                Description = Description,
                Metadata = Metadata,
                Model = Model,
                MaxTokens = MaxTokens,
                Temperature = Temperature,
                Voice = Voice,
                InterstitialPrompt = InterstitialPrompt,
                Messages = Messages
            };
        }
    }
}