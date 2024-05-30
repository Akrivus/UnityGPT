using RSG;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeechGenerator : TextToSpeechGenerator, IStreamingTextGenerator
{
    private readonly static IPromise<string> Busy = Promise<string>.Rejected(new Exception("Speech generation is already in progress."));

    public event Func<string, IPromise<string>> OnTextGenerated
    {
        add => textGenerator.OnTextGenerated += value;
        remove => textGenerator.OnTextGenerated -= value;
    }
    public event Action<string[]> OnContextReset
    {
        add => textGenerator.OnContextReset += value;
        remove => textGenerator.OnContextReset -= value;
    }

    public event Action<string> OnStreamReceived;
    public event Action<string> OnStreamEnded;

    public event Action<string> OnSpeechPlaying;
    public event Action OnSpeechComplete;

    public bool IsReady { get; private set; } = true;
    public bool IsGeneratingSpeech { get; private set; }
    public bool IsGeneratingText { get; private set; }
    public List<Message> Prompt
    {
        get => textGenerator.Prompt;
        set => textGenerator.Prompt = value;
    }

    private IStreamingTextGenerator textGenerator;
    private WordMapping wordMapping;

    private List<SpeechFragment> fragments = new List<SpeechFragment>();
    private SpeechFragment fragment = new SpeechFragment();

    public SpeechGenerator(LinkOpenAI client, IStreamingTextGenerator textGenerator, WordMapping wordMapping, string voice, Roles role = Roles.System) : base(client, "tts-1", voice, role)
    {
        this.textGenerator = textGenerator;
        this.wordMapping = wordMapping;
    }

    public IPromise<string> RespondTo(string content, params string[] context) => RespondTo(content, (text) => { });

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
            var delay = wordMapping.GetStopCodeDelay(fragment.Text);
            var seconds = fragment.Play(source);
            yield return new WaitForSeconds(seconds);
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

    public void ResetContext()
    {
        textGenerator.ResetContext();
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

    public float Play(AudioSource source)
    {
        source.clip = Clip;
        source.Play();
        var seconds = Clip.length;
        seconds *= source.pitch;
        return seconds;
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