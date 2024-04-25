using Proyecto26;
using RSG;
using System;
using System.Collections.Generic;
using UnityEngine;

public class WhisperTextGenerator : ITextGenerator
{
    private const string URI = "https://api.openai.com/v1/audio/transcriptions";

    public event Func<string, IPromise<string>> OnTextGenerated;
    public event Action<string[]> OnContextReset;

    private VoiceRecorder recorder;
    private string context = "";
    private float temperature = 0.5F;

    private List<string> messages = new List<string>();

    public List<string> Messages => messages;

    public string Context
    {
        get => messages[messages.Count - 1];
        set => messages[messages.Count - 1] = value;
    }

    public WhisperTextGenerator(VoiceRecorder recorder, float temperature, string context = "")
    {
        this.recorder = recorder;
        this.temperature = temperature;
        this.context = context;
        AddContext(context);
    }

    public IPromise<string> RespondTo(string context)
    {
        AddContext(context);
        return SendContext();
    }

    public IPromise<string> SendContext()
    {
        return recorder.Record().Then(clip => UploadAudioAndGenerateText(clip));
    }

    public void ResetContext()
    {
        OnContextReset?.Invoke(messages.ToArray());
        messages.Clear();
        messages.Add(context);
    }

    public void AddContext(string context)
    {
        messages.Add(context);
    }

    public void AddMessage(string message)
    {
        messages.Add(message);
    }

    private IPromise<string> UploadAudioAndGenerateText(AudioClip clip)
    {
        var body = new GenerateSpeechToText(context, temperature, clip.ToByteArray(recorder.NoiseFloor));
        return RestClient.Post(new RequestHelper()
        {
            Uri = URI,
            FormData = body.FormData
        })
            .Then((response) => RestClientExtensions.Deserialize<Transcription>(response.Text))
            .Then((transcription) => DispatchTranscription(transcription.Text));
    }

    private IPromise<string> DispatchTranscription(string text)
    {
        Context = text;
        return OnTextGenerated?.Invoke(text)
            ?? Promise<string>.Resolved(text);
    }
}