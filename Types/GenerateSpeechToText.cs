using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using UnityEditor;

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

    public MultipartFormDataContent AsFormData()
    {
        return new MultipartFormDataContent
        {
            { new StringContent("whisper-1"), "model" },
            { new StringContent(Prompt), "prompt" },
            { new StringContent(Temperature.ToString()), "temperature" },
            { new ByteArrayContent(Data), "file", "transcript.wav" }
        };
    }

    public static MultipartFormDataContent AsFormData(string prompt, float temperature, byte[] data)
    {
        return new GenerateSpeechToText(prompt, temperature, data).AsFormData();
    }
}

public class Transcription
{
    public string Text { get; set; }
}