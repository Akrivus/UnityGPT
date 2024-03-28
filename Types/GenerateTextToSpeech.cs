using UnityEngine;

public class GenerateTextToSpeech
{
    public string Model { get; set; } = "tts-1";
    public string Input { get; set; }
    public Voices Voice { get; set; } = Voices.Alloy;
    public Formats ResponseFormat { get; set; } = Formats.Wav;

    public GenerateTextToSpeech(string input, Voices voice)
    {
        Input = input;
        Voice = voice;
    }

    public enum Voices
    {
        Alloy,
        Echo,
        Fable,
        Onyx,
        Nova,
        Shimmer
    }

    public enum Formats
    {
        Wav,
        Mp3,
        Ogg
    }
}

public class TextToSpeech
{
    public string Text { get; set; }
    public AudioClip Speech { get; set; }

    public bool IsReady => Speech != null;

    public TextToSpeech(string text)
    {
        Text = text;
    }
}