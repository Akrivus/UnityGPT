using System;
using System.Threading.Tasks;
using UnityEngine;

public interface ITextToSpeech
{
    public event EventHandler<TextToSpeechEventArgs> TextToSpeechComplete;

    public Task<AudioClip> GenerateSpeechAsync(string text);
    public void GenerateSpeech(TextToSpeech tts);
}

public class TextToSpeechEventArgs
{
    public AudioClip Speech { get; private set; }
    public string Text { get; private set; }

    public TextToSpeechEventArgs(string text)
    {
        Text = text;
    }

    public TextToSpeechEventArgs(string text, AudioClip speech) : this(text)
    {
        Speech = speech;
    }
}