using Proyecto26;
using RSG;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class ChatGenerator : IStreamingTextGenerator, ISpeechGenerator
{
    private readonly static IPromise<string> Busy = Promise<string>.Rejected(new Exception("Speech generation is already in progress."));

    // https://phren-49cbf2cf0591.herokuapp.com
    private const string URI = "http://localhost:3000/api/v1/people/$id/chats/";

    public event Action<AudioClip> OnSpeechGenerated;
    public event Action<string> OnSpeechPlaying;
    public event Action OnSpeechComplete;
    public event Action<string> OnStreamReceived;
    public event Action<string> OnStreamEnded;
    public event Func<string, IPromise<string>> OnTextGenerated;
    public event Action<string[]> OnContextReset;

    public bool IsReady { get; private set; } = true;
    public bool IsGeneratingSpeech { get; private set; }
    public bool IsGeneratingText { get; private set; }

    public string Prompt
    {
        get => string.Empty;
        set { }
    }

    private List<AudioClip> clips = new List<AudioClip>();
    private string stream;

    private string personId;
    private string chatId;

    public ChatGenerator(string personId)
    {
        this.personId = personId;
        GetChatId();
    }

    public void ResetContext()
    {
        GetChatId().Then(_ => OnContextReset?.Invoke(new string[0]));
    }

    public IPromise<string> RespondTo(string message) => RespondTo(message, (text) => { });

    public IPromise<string> RespondTo(string message, Action<string> tokenCallback)
    {
        if (!IsReady) return Busy;
        IsReady = false;

        var uri = URI.Replace("$id", personId) + chatId;
        var sse = new ServerSentEventHandler<PhrenMessage>();
        sse.OnServerSentEvent += (e) => DispatchAudioClip(e.Data)
            .Then((text) => tokenCallback(text));

        stream = string.Empty;

        return RestClient.Put(new RequestHelper
        {
            Uri = uri,
            BodyString = RestClientExtensions.Serialize(new PhrenMessage(message)),
            DownloadHandler = sse
        }).Then(_ => DispatchGeneratedText(stream));
    }

    public IEnumerator PlaySpeech(AudioSource source, int i = 0)
    {
        yield return new WaitUntil(() => clips.Count > i);
        for (var _ = i; i < clips.Count; i++)
        {
            var clip = clips[i];
            yield return new WaitUntil(() => clip.loadState == AudioDataLoadState.Loaded);
            IsGeneratingSpeech = true;
            source.clip = clip;
            source.Play();
            var seconds = clip.length * source.pitch;
            yield return new WaitForSeconds(seconds);
            IsGeneratingSpeech = false;
        }
        if (IsGeneratingText)
            yield return PlaySpeech(source, i);
        IsReady = true;
        OnSpeechComplete?.Invoke();
    }

    public IPromise<string> RecordAndRespondTo(VoiceRecorder recorder, string prompt)
    {
        return recorder.Record().Then((data) => RespondTo(recorder, data, prompt));
    }

    private IPromise<string> RespondTo(VoiceRecorder recorder, float[] data, string prompt)
    {
        if (!IsReady) return Busy;
        IsReady = false;

        var bytes = AudioClipExtensions.ToByteArray(data, recorder.Channels, recorder.Frequency);
        var body = new PhrenMessageAudio(bytes, prompt);

        var uri = URI.Replace("$id", personId) + chatId;
        var sse = new ServerSentEventHandler<PhrenMessage>();
        sse.OnServerSentEvent += (e) => DispatchAudioClip(e.Data);

        return RestClient.Put(new RequestHelper
        {
            Uri = uri,
            FormData = body.FormData,
            DownloadHandler = sse
        }).Then((_) => DispatchGeneratedText(stream));
    }

    private IPromise<string> DispatchAudioClip(PhrenMessage message)
    {
        if (message.Content != null) stream += message.Content;
        foreach (var audioFile in message.AudioFiles)
            RestClientExtensions.GetAudioClip(audioFile.Url).Then(clip =>
            {
                clips.Add(clip);
                OnSpeechGenerated?.Invoke(clip);
            });
        return Promise<string>.Resolved(message.Content);
    }

    private string DispatchGeneratedText(string message)
    {
        OnStreamEnded?.Invoke(message);
        return message;
    }

    private IPromise<string> GetChatId()
    {
        if (!IsReady) return Busy;
        IsReady = false;
        return RestClientExtensions.Post<PhrenChat>(URI.Replace("$id", personId), "").Then(chat =>
        {
            chatId = chat.Id;
            IsReady = true;
            return chatId;
        });
    }
}

public class PhrenChat
{
    public string Id { get; set; }
}

public class PhrenMessageAudio
{
    public byte[] Data { get; set; }
    public string Prompt { get; set; }

    public PhrenMessageAudio(byte[] data, string prompt)
    {
        Data = data;
        Prompt = prompt;
    }

    public WWWForm FormData => GenerateFormData(new WWWForm());

    private WWWForm GenerateFormData(WWWForm form)
    {
        form.AddField("stream", "on");
        form.AddField("tts", "on");
        form.AddField("message_audio[prompt]", Prompt);
        form.AddBinaryData("message_audio[file]", Data, "speech.wav", "audio/wav");
        return form;
    }


}

public class PhrenMessage
{
    public string Content { get; set; }
    public Message.Roles Role { get; set; }
    public PhrenAudioFile[] AudioFiles { get; set; }     
    
    public PhrenMessage(string content)
    {
        Content = content;
        Role = Message.Roles.User;
    }
}

public class PhrenAudioFile
{
    public string Url { get; set; }
}