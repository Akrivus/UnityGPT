using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public interface ITextToSpeech
{
    public event EventHandler<AudioClip> TextToSpeechComplete;

    public Task<AudioClip> GenerateSpeechAsync(string text);

    public void GenerateSpeech(TextToSpeech tts);
}
