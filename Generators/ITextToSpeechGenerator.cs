using RSG;
using UnityEngine;

public interface ITextToSpeechGenerator
{
    public IPromise<AudioClip> Generate(string text);
}