using RSG;
using System;
using System.Collections;
using UnityEngine;

public interface ISpeechGenerator
{
    public event Action<string> OnSpeechPlaying;
    public event Action OnSpeechComplete;

    public bool IsReady { get; }

    public IPromise<string> RespondTo(string message);
    public IEnumerator PlaySpeech(AudioSource source, int i = 0);
}