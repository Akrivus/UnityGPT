using System;
using System.Threading.Tasks;
using UnityEngine;

public interface ITextToSpeech
{
    public event EventHandler<TextToSpeechEvent> TextToSpeechComplete;

    public Task<AudioClip> GenerateSpeechAsync(string text);
    public void GenerateSpeech(TextToSpeech tts);
}

public class TextToSpeechEvent
{
    public AudioClip Speech { get; private set; }
    public string Text { get; private set; }

    public TextToSpeechEvent(string text)
    {
        Text = text;
    }

    public TextToSpeechEvent(string text, AudioClip speech) : this(text)
    {
        Speech = speech;
    }
}