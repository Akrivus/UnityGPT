using Newtonsoft.Json;
using UnityEngine;

public class GenerateSpeechToText
{
    public string Prompt { get; set; }
    public float Temperature { get; set; }

    [JsonIgnore]
    public byte[] Data { get; set; }

    public GenerateSpeechToText(string prompt, float temperature, byte[] data)
    {
        Prompt = prompt;
        Temperature = temperature;
        Data = data;
    }

    public WWWForm FormData => GenerateFormData(new WWWForm());

    private WWWForm GenerateFormData(WWWForm form)
    {
        form.AddField("model", "whisper-1");
        form.AddField("prompt", Prompt);
        form.AddField("temperature", Temperature.ToString());
        form.AddBinaryData("file", Data, "speech.wav", "audio/wav");
        return form;
    }
}

public class Transcription
{
    public string Text { get; set; }
}