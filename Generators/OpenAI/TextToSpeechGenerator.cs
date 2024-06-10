using RSG;
using System;
using UnityEngine;

public class TextToSpeechGenerator : ITextToSpeechGenerator
{
    public event Action<AudioClip> OnSpeechGenerated;

    public string Model { get; protected set; } = "tts-1";
    public string Voice { get; set; } = "echo";
    public float Speed { get; set; } = 1f;

    protected LinkOpenAI client;

    private Roles role = Roles.System;

    public TextToSpeechGenerator(LinkOpenAI api, string model, string voice, float speed = 1.0f, Roles role = Roles.System)
    {
        this.client = api;
        Model = model;
        Voice = voice;
        this.role = role;
    }

    public IPromise<AudioClip> Generate(string text)
    {
        var body = RestClientExtensions.Serialize(new GenerateTextToSpeech(text, Voice, Model, Speed, role));
        return client.Post(client.Uri_Speech, body, "application/json")
            .Then((helper) => Generate(helper.Data));
    }

    private AudioClip Generate(byte[] data)
    {
        var samples = new float[data.Length / AudioClipExtensions.BYTES_IN_FLOATS - 44];
        for (int i = 0; i < samples.Length; i++)
            samples[i] = (float)(BitConverter.ToInt32(data, i * AudioClipExtensions.BYTES_IN_FLOATS + 44)) / int.MaxValue;
        var clip = AudioClip.Create("Speech", samples.Length, 1, 24000, false);
        clip.SetData(samples, 0);
        OnSpeechGenerated?.Invoke(clip);
        return clip;
    }
}