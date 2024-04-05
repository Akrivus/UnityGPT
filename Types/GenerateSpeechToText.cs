using Newtonsoft.Json;
using System.IO;
using System.Net.Http;

public class GenerateSpeechToText
{
    public SpeechModel Model { get; set; }
    public string Prompt { get; set; }
    public float Temperature { get; set; }

    public MultipartFormDataContent WithFile(string filename)
    {
        var content = new MultipartFormDataContent();
        var name = Path.GetFileName(filename);
        content.Add(new ByteArrayContent(File.ReadAllBytes(filename)), "file", name);
        content.Add(new StringContent(JsonConvert.SerializeObject(Model)), "model");
        content.Add(new StringContent(Prompt), "prompt");
        content.Add(new StringContent(Temperature.ToString()), "temperature");
        return content;
    }
}

public class Transcription
{
    public string Text { get; set; }
}

public enum SpeechModel
{
    [JsonProperty("whisper-1")]
    Whisper_1,
}