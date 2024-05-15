
public class GenerateTextToSpeech
{
    public string Model { get; set; }
    public string Input { get; set; }
    public string Voice { get; set; } = "echo";
    public string ResponseFormat { get; set; } = "wav";
    public Roles Role { get; set; } = Roles.System;

    public GenerateTextToSpeech(string input, string voice, string model, Roles role = Roles.System)
    {
        Input = input;
        Voice = voice;
        Model = model;
        Role = role;
    }

    public bool ShouldSerializeRole() => Role != Roles.System;
}