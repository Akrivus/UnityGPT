using Proyecto26;
using RSG;
using System;
using UnityEngine;
using static GenerateTextToSpeech;

public class TextToSpeechGenerator : ITextToSpeechGenerator
{
    private const string URI = "https://api.openai.com/v1/audio/speech";

    public event Action<AudioClip> OnSpeechGenerated;

    public TextToSpeechModel Model { get; protected set; } = TextToSpeechModel.TTS_1;
    public Voices Voice { get; set; } = Voices.Echo;

    public TextToSpeechGenerator(TextToSpeechModel model, Voices voice)
    {
        Model = model;
        Voice = voice;
    }

    public IPromise<AudioClip> Generate(string text)
    {
        var body = RestClientExtensions.Serialize(new GenerateTextToSpeech(text, Voice, Model));
        return RestClient.Post(URI, body)
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