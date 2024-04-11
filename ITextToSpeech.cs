using RSG;
using UnityEngine;

using static GenerateTextToSpeech;

public interface ITextToSpeech
{
    public IPromise<TextToSpeech> Say(string text);
}

public class TextToSpeech
{
    public string Text { get; set; }
    public AudioClip Speech { get; set; }
    public Promise<bool> Play { get; set; }

    public TextToSpeech()
    {
        Play = new Promise<bool>();
    }

    public TextToSpeech(string text) : this()
    {
        Text = text;
    }

    public TextToSpeech(string text, AudioClip speech) : this(text)
    {
        Speech = speech;
    }
}

public class TextToSpeechEventArgs
{
    public TextToSpeech TextToSpeech;
    public string Text => TextToSpeech.Text;
    public AudioClip Speech => TextToSpeech.Speech;

    public TextToSpeechEventArgs(TextToSpeech tts)
    {
        TextToSpeech = tts;
    }
}