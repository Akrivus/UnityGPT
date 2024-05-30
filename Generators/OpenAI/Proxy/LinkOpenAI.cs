using Proyecto26;
using RSG;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class LinkOpenAI : MonoBehaviour
{
    public static string DefaultUri => "https://api.openai.com/v1/";
    public static string DefaultToken => Environment.GetEnvironmentVariable("OPENAI_API_KEY");

    public event Action<SessionData> OnSuccessfulLink;

    public string Uri_Speech => apiEndpoint + "audio/speech";
    public string Uri_Transcriptions => apiEndpoint + "audio/transcriptions";
    public string Uri_Chat => apiEndpoint + "chat/completions";
    public string Uri_Embeddings => apiEndpoint + "embeddings";
    public string Uri_Login => apiEndpoint + "api";

    [SerializeField]
    private string apiEndpoint;
    [SerializeField]
    private string accessToken;
    [SerializeField]
    private DefaultSessionData defaults;

    private void Awake()
    {
        if (Application.absoluteURL.IndexOf('?') > -1)
            accessToken = Application.absoluteURL.Split('?')[1].Split('=')[1];
        if (string.IsNullOrEmpty(accessToken))
            accessToken = DefaultToken;
        if (accessToken.StartsWith("sk-"))
            AutoLogin();
    }

    public IPromise<SessionData> Login<T>(T context) where T : IPhrenContext
        => Post<SessionData>(Uri_Login, context)
        .Then((session) => AutoLogin(session));

    public IPromise<SessionData> Login()
        => Login(new PromptData(accessToken));

    public IPromise<SessionData> AutoLogin(SessionData session = null)
    {
        if (session == null)
            session = defaults.Build(DefaultUri, accessToken);
        accessToken = session.AccessToken;
        OnSuccessfulLink?.Invoke(session);
        return Promise<SessionData>.Resolved(session);
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
    private struct DefaultSessionData
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

        public SessionData Build(string uri, string token)
        {
            return new SessionData
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