using Newtonsoft.Json;
using UnityEngine;

public class GenerateTextToSpeech
{
    public VoiceModel Model { get; set; }
    public string Input { get; set; }
    public Voices Voice { get; set; } = Voices.Alloy;
    public Formats ResponseFormat { get; set; } = Formats.Wav;

    public GenerateTextToSpeech(string input, Voices voice, VoiceModel model)
    {
        Input = input;
        Voice = voice;
        Model = model;
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

public enum VoiceModel
{
    [JsonProperty("tts-1")]
    TTS_1,
    [JsonProperty("tts-1-hd")]
    TTS_1HD,
}