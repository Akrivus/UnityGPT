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

    public event Action<float[]> OnSpeechReceived;

    private VoiceRecorder recorder;
    private float temperature = 0.5F;
    private string context;

    private List<string> messages = new List<string>();

    public List<string> Messages => messages;

    public string Prompt
    {
        get => messages[messages.Count - 1];
        set => AddContext(value);
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
        return recorder.Record().Then(data => UploadAudioAndGenerateText(data));
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

    private IPromise<string> UploadAudioAndGenerateText(float[] data)
    {
        OnSpeechReceived?.Invoke(data);

        var bytes = AudioClipExtensions.ToByteArray(data, recorder.Channels, recorder.Frequency);
        var body = new GenerateSpeechToText(Prompt, temperature, bytes);

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