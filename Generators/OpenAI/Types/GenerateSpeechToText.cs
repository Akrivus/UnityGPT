using Newtonsoft.Json;
using UnityEngine;

public class GenerateSpeechToText
{
    public string Prompt { get; set; }
    public float Temperature { get; set; }
    public Roles Role { get; set; } = Roles.System;
    public byte[] Data { get; set; }

    public GenerateSpeechToText(string prompt, float temperature, byte[] data, Roles role = Roles.System)
    {
        Prompt = prompt;
        Temperature = temperature;
        Data = data;
        Role = role;
    }

    public WWWForm FormData => GenerateFormData(new WWWForm());

    private WWWForm GenerateFormData(WWWForm form)
    {
        form.AddField("model", "whisper-1");
        form.AddField("temperature", Temperature.ToString());
        form.AddBinaryData("file", Data, "speech.wav", "audio/wav");
        return form;
    }
}

public class Transcription
{
    public string Text { get; set; }
}