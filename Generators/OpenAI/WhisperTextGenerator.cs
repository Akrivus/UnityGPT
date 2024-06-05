using Proyecto26;
using RSG;
using System;
using System.Collections.Generic;
using UnityEngine;

public class WhisperTextGenerator : ITextGenerator
{
    public event Func<string, IPromise<string>> OnTextGenerated;
    public event Action<ITextGenerator> OnContextReset;
    public event Action<float[]> OnSpeechReceived;

    public string LastMessage { get; set; }

    public List<Message> Prompt
    {
        get => promptGenerator.Prompt;
        set => promptGenerator.Prompt = value;
    }

    private VoiceRecorder recorder;
    private string prompt;
    private string model;
    private int maxTokens;
    private float temperature = 0.5F;

    private TextGenerator promptGenerator;
    private bool promptGenerated = false;

    private LinkOpenAI client;

    private Roles role = Roles.System;

    public WhisperTextGenerator(LinkOpenAI api, VoiceRecorder recorder, string prompt, string model, int maxTokens, float temperature, Roles role = Roles.System)
    {
        this.client = api;
        this.recorder = recorder;
        this.prompt = prompt;
        this.model = model;
        this.maxTokens = maxTokens;
        this.temperature = temperature;
        this.role = role;
        SetPromptGenerator(prompt);
    }

    public IPromise<string> RespondTo(string message, params string[] context)
    {
        if (promptGenerated) return SendContext();
        message = string.IsNullOrEmpty(message) ? prompt : message;
        return SetContext(message)
            .Then(SendContext);
    }

    public ITextGenerator Fork(string prompt)
    {
        return new WhisperTextGenerator(client, recorder, prompt, model, maxTokens, temperature, role);
    }

    public IPromise<string> SendContext()
    {
        return recorder.Record().Then(data => UploadAudioAndGenerateText(data));
    }

    public void ResetContext()
    {
        OnContextReset?.Invoke(this);
    }

    public IPromise SetContext(string message)
    {
        return promptGenerator.RespondTo(message)
            .Then(SetPrompt);
    }

    private void SetPrompt(string prompt)
    {
        Debug.Log("Cheat Prompt:\n" + prompt);
        this.prompt = prompt;
        promptGenerated = true;
    }

    private void SetPromptGenerator(string prompt)
    {
        promptGenerator = new TextGenerator(client, prompt, model, maxTokens, temperature);
    }

    private IPromise<string> UploadAudioAndGenerateText(float[] data)
    {
        OnSpeechReceived?.Invoke(data);

        var bytes = AudioClipExtensions.ToByteArray(data, recorder.Channels, recorder.Frequency);
        var body = new GenerateSpeechToText(prompt, temperature, bytes, role);

        return client.Post(new RequestHelper()
        {
            Uri = client.Uri_Transcriptions,
            FormData = body.FormData
        })
            .Then((response) => RestClientExtensions.Deserialize<Transcription>(response.Text))
            .Then((transcription) => DispatchTranscription(transcription.Text));
    }

    private IPromise<string> DispatchTranscription(string text)
    {
        LastMessage = text;
        promptGenerated = false;
        return OnTextGenerated?.Invoke(text)
            ?? Promise<string>.Resolved(text);
    }
}