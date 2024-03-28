using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Windows;

public class TextToSpeechGenerator : ITextToSpeech
{
    public event EventHandler<AudioClip> TextToSpeechComplete;

    GenerateTextToSpeech.Voices voice = GenerateTextToSpeech.Voices.Echo;

    public TextToSpeechGenerator(GenerateTextToSpeech.Voices voice)
    {
        this.voice = voice;
    }

    public async Task<AudioClip> GenerateSpeechAsync(string text)
    {
        var body = QuickJSON.Serialize(new GenerateTextToSpeech(text, voice));
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
        tts.Speech = await GenerateSpeechAsync(tts.Text);
        TextToSpeechComplete?.Invoke(this, tts.Speech);
    }
}
