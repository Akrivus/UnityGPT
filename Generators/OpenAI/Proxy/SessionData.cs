using System.Collections.Generic;

public class SessionData
{
    public string AccessToken { get; set; }
    public string Href { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Metadata { get; set; }
    public string Model { get; set; }
    public int MaxTokens { get; set; }
    public float Temperature { get; set; }
    public string Voice { get; set; }
    public string InterstitialPrompt { get; set; }
    public List<Message> Messages { get; set; }
}