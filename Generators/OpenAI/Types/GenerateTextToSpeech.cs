
public class GenerateTextToSpeech
{
    public string Model { get; set; }
    public string Input { get; set; }
    public string Voice { get; set; } = "echo";
    public string ResponseFormat { get; set; } = "wav";

    public GenerateTextToSpeech(string input, string voice, string model)
    {
        Input = input;
        Voice = voice;
        Model = model;
    }
}