
public class GenerateTextToSpeech
{
    public string Model { get; set; }
    public string Input { get; set; }
    public string Voice { get; set; } = "echo";
    public float Speed { get; set; } = 1.0f;
    public string ResponseFormat { get; set; } = "wav";
    public Roles Role { get; set; } = Roles.System;

    public GenerateTextToSpeech(string input, string voice, string model, float speed = 1.0f, Roles role = Roles.System)
    {
        Input = input;
        Voice = voice;
        Model = model;
        Speed = speed;
        Role = role;
    }

    public bool ShouldSerializeRole() => Role != Roles.System;
}