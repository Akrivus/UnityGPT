using System;
using System.Collections;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

public class WhisperTextGenerator : IText
{
    VoiceRecorder recorder;
    string prompt = "";
    float temperature = 0.5F;
    
    string _content;

    public event EventHandler<TextEventArgs> TextStart;
    public event EventHandler<TextEventArgs> TextComplete;

    public string Prompt => prompt;

    public WhisperTextGenerator(VoiceRecorder recorder, string prompt, float temperature)
    {
        this.recorder = recorder;
        this.prompt = prompt;
        this.temperature = temperature;
    }

    public async Task<string> GenerateTextAsync(string content)
    {
        recorder.OnRecordStop -= UploadAudioAndGenerateText;
        recorder.OnRecordStop += UploadAudioAndGenerateText;
        recorder.Record();
        prompt = content;
        _content = string.Empty;
        while (string.IsNullOrEmpty(_content))
            await Task.Yield();
        TextComplete?.Invoke(this, new TextEventArgs(_content));
        return _content;
    }

    public IEnumerator GenerateText(string content)
    {
        recorder.OnRecordStop -= UploadAudioAndGenerateText;
        recorder.OnRecordStop += UploadAudioAndGenerateText;
        recorder.Record();
        prompt = content;
        _content = string.Empty;
        yield return new WaitUntil(() => !string.IsNullOrEmpty(_content));
        TextComplete?.Invoke(this, new TextEventArgs(_content));
    }

    public void ClearMessages()
    {

    }

    async void UploadAudioAndGenerateText(AudioClip clip)
    {
        TextStart?.Invoke(this, new TextEventArgs(null));
        var res = await ChatAgent.API.SendMultiPartAsync<Transcription>(HttpMethod.Post, "audio/transcriptions", GenerateSpeechToText.AsFormData(prompt, temperature, clip.ToByteArray(recorder.NoiseFloor)));
        _content = res.Text;
    }
}