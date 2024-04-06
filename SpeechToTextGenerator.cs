using System;
using System.Collections;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

public class SpeechToTextGenerator : IText
{
    VoiceRecorder recorder;
    string prompt = "";
    SpeechToTextModel model = SpeechToTextModel.Whisper_1;
    float temperature = 0.5F;
    
    string _content;

    public event EventHandler<TextEvent> TextStart;
    public event EventHandler<TextEvent> TextComplete;

    public string Prompt => prompt;

    public SpeechToTextGenerator(VoiceRecorder recorder, string prompt, SpeechToTextModel model, float temperature)
    {
        this.recorder = recorder;
        this.prompt = prompt;
        this.model = model;
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
        TextComplete?.Invoke(this, new TextEvent(_content));
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
        TextComplete?.Invoke(this, new TextEvent(_content));
    }

    public void ClearMessages()
    {

    }

    async void UploadAudioAndGenerateText(AudioClip clip)
    {
        TextStart?.Invoke(this, new TextEvent(null));
        var res = await ChatGenerator.API.SendMultiPartAsync<Transcription>(HttpMethod.Post, "audio/transcriptions", GenerateSpeechToText.AsFormData(model, prompt, temperature, clip.ToByteArray(recorder.NoiseFloor)));
        _content = res.Text;
    }
}