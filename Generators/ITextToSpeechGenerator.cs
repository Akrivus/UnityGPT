using RSG;
using System;
using UnityEngine;

public interface ITextToSpeechGenerator
{
    public IPromise<AudioClip> Generate(string text);

    public event Action<AudioClip> OnSpeechGenerated;
}