using Proyecto26;
using RSG;
using System;
using UnityEngine;

public class WhisperTextGenerator : IText
{
    const string URI = "https://api.openai.com/v1/audio/transcriptions";

    public event EventHandler<TextEventArgs> OnGenerated;

    VoiceRecorder recorder;
    string prompt = "";
    float temperature = 0.5F;

    public string Prompt
    {
        get => prompt;
        set => prompt = value;
    }

    public WhisperTextGenerator(VoiceRecorder recorder, string prompt, float temperature)
    {
        this.recorder = recorder;
        this.prompt = prompt;
        this.temperature = temperature;
    }

    public IPromise<string> Ask(string context)
    {
        Tell(context);
        return Listen();
    }

    public IPromise<string> Listen()
    {
        var clip = new Promise<AudioClip>();
        recorder.Record(clip);
        return clip.Then(clip => UploadAudioAndGenerateText(clip));
    }

    public void Tell(string context)
    {
        prompt = context;
    }

    public void ResetContext()
    {
        prompt = string.Empty;
    }

    private IPromise<string> UploadAudioAndGenerateText(AudioClip clip)
    {
        var body = new GenerateSpeechToText(prompt, temperature,
            clip.ToByteArray(recorder.NoiseFloor));
        return RestClient.Post(new RequestHelper()
        {
            Uri = URI,
            FormData = body.FormData
        })
            .Then((response) => RestClientExtensions.Deserialize<Transcription>(response.Text))
            .Then((transcription) => DispatchTranscription(transcription.Text));
    }

    private string DispatchTranscription(string text)
    {
        OnGenerated?.Invoke(this, new TextEventArgs(text));
        return text;
    }
}