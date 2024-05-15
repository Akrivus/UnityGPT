using RSG;
using System;
using UnityEngine;

public class TextToSpeechGenerator : ITextToSpeechGenerator
{
    public event Action<AudioClip> OnSpeechGenerated;

    public string Model { get; protected set; } = "tts-1";
    public string Voice { get; set; } = "echo";

    protected PhrenProxyClient api;

    private Roles role = Roles.System;

    public TextToSpeechGenerator(PhrenProxyClient api, string model, string voice, Roles role = Roles.System)
    {
        this.api = api;
        Model = model;
        Voice = voice;
        this.role = role;
    }

    public IPromise<AudioClip> Generate(string text)
    {
        var body = RestClientExtensions.Serialize(new GenerateTextToSpeech(text, Voice, Model, role));
        return api.Post(api.Uri_Speech, body, "application/json")
            .Then((helper) => Generate(helper.Data));
    }

    private AudioClip Generate(byte[] data)
    {
        var samples = new float[data.Length / 4];
        for (int i = 0; i < samples.Length; i++)
            if (i > 44) samples[i] = (float)(BitConverter.ToInt32(data, i * 4)) / int.MaxValue;
        var clip = AudioClip.Create("Speech", samples.Length, 1, 12000, false);
        clip.SetData(samples, 0);
        OnSpeechGenerated?.Invoke(clip);
        return clip;
    }
}