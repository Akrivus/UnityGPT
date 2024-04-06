using System;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

public class TextToSpeechGenerator : ITextToSpeech
{
    TextToSpeechModel model = TextToSpeechModel.TTS_1;
    GenerateTextToSpeech.Voices voice = GenerateTextToSpeech.Voices.Echo;

    public event EventHandler<TextToSpeechEvent> TextToSpeechStart;
    public event EventHandler<TextToSpeechEvent> TextToSpeechComplete;

    public TextToSpeechGenerator(TextToSpeechModel model, GenerateTextToSpeech.Voices voice)
    {
        this.model = model;
        this.voice = voice;
    }

    public async Task<AudioClip> GenerateSpeechAsync(string text)
    {
        var body = QuickJSON.Serialize(new GenerateTextToSpeech(text, voice, model));
        var res = await ChatGenerator.API.CleanSendAsync(HttpMethod.Post, "audio/speech", body);
        var bytes = await res.Content.ReadAsByteArrayAsync();
        var samples = new float[bytes.Length / 4];
        for (int i = 0; i < samples.Length; i++)
            samples[i] = (float)(BitConverter.ToInt32(bytes, i * 4)) / int.MaxValue;
        var clip = AudioClip.Create("Speech", samples.Length, 1, 12000, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public async void GenerateSpeech(TextToSpeech tts)
    {
        TextToSpeechStart?.Invoke(this, new TextToSpeechEvent(tts.Text));
        tts.Speech = await GenerateSpeechAsync(tts.Text);
        TextToSpeechComplete?.Invoke(this, new TextToSpeechEvent(tts.Text, tts.Speech));
    }
}
