using RSG;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeechGenerator : TextToSpeechGenerator, IStreamingTextGenerator
{
    private readonly static IPromise<string> Busy = Promise<string>.Rejected(new Exception("Speech generation is already in progress."));

    public event Func<string, IPromise<string>> OnTextGenerated;
    public event Action<string[]> OnContextReset;

    public event Action<string> OnStreamReceived;
    public event Action<string> OnStreamEnded;

    public event Action<string> OnSpeechPlaying;
    public event Action OnSpeechComplete;

    public bool IsReady { get; private set; } = true;
    public bool IsGeneratingSpeech { get; private set; }
    public bool IsGeneratingText { get; private set; }
    public string Context
    {
        get => textGenerator.Context;
        set => textGenerator.Context = value;
    }

    private IStreamingTextGenerator textGenerator;
    private WordMapping wordMapping;
    private float pitch = 1.0f;

    private List<SpeechFragment> fragments = new List<SpeechFragment>();
    private SpeechFragment fragment = new SpeechFragment();

    public SpeechGenerator(IStreamingTextGenerator textGenerator, WordMapping wordMapping, TextToSpeechModel textToSpeechModel, GenerateTextToSpeech.Voices voice, float pitch = 1.0f) : base(textToSpeechModel, voice)
    {
        this.textGenerator = textGenerator;
        this.textGenerator.OnTextGenerated += (text) => OnTextGenerated?.Invoke(text);
        this.textGenerator.OnContextReset += (context) => OnContextReset?.Invoke(context);
        this.wordMapping = wordMapping;
        this.pitch = pitch;
    }

    public IPromise<string> RespondTo(string content) => RespondTo(content, (text) => { });

    public IPromise<string> RespondTo(string content, Action<string> tokenCallback)
    {
        if (!IsReady) return Busy;
        IsReady = false;
        IsGeneratingText = true;
        fragments.Clear();
        return textGenerator.RespondTo(content, GenerateSpeechFragments + tokenCallback)
            .Then(Respond);
    }

    public IEnumerator PlaySpeech(AudioSource source, int i = 0)
    {
        yield return new WaitUntil(() => fragments.Count > i);
        for (var _ = i; i < fragments.Count; i++)
        {
            var fragment = fragments[i];
            yield return new WaitUntil(fragment.IsReady);
            IsGeneratingSpeech = true;
            OnSpeechPlaying?.Invoke(fragment.Text);
            yield return new WaitForSeconds(fragment.Play(source, pitch));
            IsGeneratingSpeech = false;
        }
        if (IsGeneratingText)
            yield return PlaySpeech(source, i);
        IsReady = true;
        OnSpeechComplete?.Invoke();
    }

    private void GenerateSpeechFragments(string token)
    {
        fragment += token;
        if (wordMapping.MatchStopCode(token))
            GenerateSpeechFragment();
    }

    private void GenerateSpeechFragment()
    {
        OnStreamReceived?.Invoke(fragment.Text);
        fragment.Generate(Generate(fragment.Text));
        fragments.Add(fragment);
        fragment = new SpeechFragment();
    }

    private string Respond(string text)
    {
        IsGeneratingText = false;
        OnStreamEnded?.Invoke(text);
        return text;
    }

    public IPromise<string> SendContext()
    {
        return textGenerator.SendContext();
    }

    public void ResetContext()
    {
        textGenerator.ResetContext();
    }

    public void AddContext(string context)
    {
        textGenerator.AddContext(context);
    }

    public void AddMessage(string message)
    {
        textGenerator.AddMessage(message);
    }
}

public class SpeechFragment
{
    public string Text { get; set; }
    public AudioClip Clip { get; set; }

    public SpeechFragment()
    {
        Text = string.Empty;
    }

    public float Play(AudioSource source, float pitch)
    {
        source.pitch = pitch;
        source.PlayOneShot(Clip);
        return Clip.length * pitch;
    }

    public void Generate(IPromise<AudioClip> getClip)
    {
        getClip.Then((clip) => Clip = clip);
    }

    public bool IsReady()
    {
        return Clip != null;
    }

    public static SpeechFragment operator +(SpeechFragment fragment, string token)
    {
        fragment.Text += token;
        return fragment;
    }
}