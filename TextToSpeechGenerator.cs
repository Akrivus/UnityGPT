using Proyecto26;
using RSG;
using System;
using UnityEngine;

using static GenerateTextToSpeech;

public class TextToSpeechGenerator : ITextToSpeech
{
    const string URI = "https://api.openai.com/v1/audio/speech";

    public event EventHandler<TextToSpeechEventArgs> OnGenerated;

    TextToSpeechModel model = TextToSpeechModel.TTS_1;
    Voices voice = Voices.Echo;

    string text;

    public TextToSpeechGenerator(TextToSpeechModel model, Voices voice)
    {
        this.model = model;
        this.voice = voice;
    }

    public IPromise<TextToSpeech> Say(string text)
    {
        var body = RestClientExtensions.Serialize(new GenerateTextToSpeech(text, voice, model));
        return RestClient.Post(URI, body)
            .Then(helper => Generate(text, helper.Data))
            .Then(tts => DispatchTextToSpeech(tts));
    }

    private TextToSpeech Generate(string text, byte[] data)
    {
        var samples = new float[data.Length / 4];
        for (int i = 0; i < samples.Length; i++)
            samples[i] = (float)(BitConverter.ToInt32(data, i * 4)) / int.MaxValue;
        var clip = AudioClip.Create("Speech", samples.Length, 1, 12000, false);
        clip.SetData(samples, 0);
        return new TextToSpeech(text, clip);
    }

    private TextToSpeech DispatchTextToSpeech(TextToSpeech tts)
    {
        OnGenerated?.Invoke(this, new TextToSpeechEventArgs(tts));
        return tts;
    }
}