using Proyecto26;
using RSG;
using System;
using UnityEngine;

public class WhisperTextGenerator : ITextGenerator
{
    private const string URI = "https://api.openai.com/v1/audio/transcriptions";

    public event Func<string, IPromise<string>> OnTextGenerated;
    public event Action<string[]> OnContextReset;

    private VoiceRecorder recorder;
    private string context = "";
    private float temperature = 0.5F;

    public string Context
    {
        get => context;
        set => context = value;
    }

    public WhisperTextGenerator(VoiceRecorder recorder, string context, float temperature)
    {
        this.recorder = recorder;
        this.context = context;
        this.temperature = temperature;
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
        OnContextReset?.Invoke(new string[0]);
        context = string.Empty;
    }

    public void AddContext(string context)
    {
        this.context += context;
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
        return OnTextGenerated?.Invoke(text)
            ?? Promise<string>.Resolved(text);
    }
}