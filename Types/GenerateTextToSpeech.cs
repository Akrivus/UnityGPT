
public class GenerateTextToSpeech
{
    public TextToSpeechModel Model { get; set; }
    public string Input { get; set; }
    public Voices Voice { get; set; } = Voices.Alloy;
    public Formats ResponseFormat { get; set; } = Formats.Wav;

    public GenerateTextToSpeech(string input, Voices voice, TextToSpeechModel model)
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