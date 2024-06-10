using RSG;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeechGenerator : TextToSpeechGenerator, IStreamingTextGenerator
{
    public event Func<string, IPromise<string>> OnTextGenerated
    {
        add => textGenerator.OnTextGenerated += value;
        remove => textGenerator.OnTextGenerated -= value;
    }

    public event Action<ITextGenerator> OnContextReset
    {
        add => textGenerator.OnContextReset += value;
        remove => textGenerator.OnContextReset -= value;
    }

    public event Action<string> OnStreamReceived;
    public event Action<string> OnStreamEnded;

    public event Action<string> OnSpeechPlaying;
    public event Action OnSpeechComplete;
    public event Func<SpeechGenerator, IPromise> OnBeforeContextReset;

    public int MaxResponseTime { get; set; } = 15;
    public int MaxResponseTimeInMS => MaxResponseTime * 1000;

    public bool IsReady { get; private set; } = true;
    public bool IsGeneratingSpeech { get; private set; }
    public bool IsGeneratingText { get; private set; }
    public List<Message> Prompt
    {
        get => textGenerator.Prompt;
        set => textGenerator.Prompt = value;
    }

    public bool TextOnly { get; set; }

    public IStreamingTextGenerator Text => textGenerator;

    public string LastMessage => textGenerator.LastMessage;

    private IStreamingTextGenerator textGenerator;
    private WordMapping wordMapping;

    private List<SpeechFragment> fragments = new List<SpeechFragment>();
    private SpeechFragment fragment = new SpeechFragment();

    public SpeechGenerator(LinkOpenAI client, IStreamingTextGenerator textGenerator, WordMapping wordMapping, string voice, float speed = 1.0f, Roles role = Roles.System) : base(client, "tts-1", voice, speed, role)
    {
        this.textGenerator = textGenerator;
        this.wordMapping = wordMapping;
    }

    public IPromise<string> RespondTo(string content, params string[] args) => RespondTo(content, (text) => { }, args);

    public IPromise<string> RespondTo(string content, Action<string> tokenCallback, params string[] args)
    {
        IsReady = false;
        IsGeneratingText = true;
        fragments.Clear();
        return textGenerator.RespondTo(content, GenerateSpeechFragments + tokenCallback, args)
            .Then(Respond);
    }

    public IEnumerator PlaySpeech(AudioSource source, int i = 0, float t = 0)
    {
        if (TextOnly) yield break;
        yield return new WaitFor(() => fragments.Count > i, MaxResponseTimeInMS);

        for (var _ = i; i < fragments.Count; i++)
            yield return PlayFragment(fragments[i], source);
        if (IsGeneratingText && t < MaxResponseTime)
            yield return PlaySpeech(source, i, t + Time.deltaTime);
        IsReady = true;
        OnSpeechComplete?.Invoke();
    }

    private IEnumerator PlayFragment(SpeechFragment fragment, AudioSource source)
    {
        if (TextOnly) yield break;
        yield return new WaitFor(fragment.HasClip, 15000);
        if (!fragment.HasClip())
            yield break;

        IsGeneratingSpeech = true;

        OnSpeechPlaying?.Invoke(fragment.Text);

        var time = fragment.Play(source);
        time += wordMapping.GetStopCodeDelay(fragment.Text);
        yield return new WaitForSeconds(time);

        IsGeneratingSpeech = false;
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
        if (TextOnly) return;
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

    public ITextGenerator Fork(string prompt)
    {
        return new SpeechGenerator(client, (IStreamingTextGenerator) textGenerator.Fork(prompt), wordMapping, Voice);
    }

    public void ResetContext()
    {
        OnBeforeContextReset(this)?.Then(textGenerator.ResetContext);
        if (OnBeforeContextReset == null)
            textGenerator.ResetContext();
    }

    public void SetReady()
    {
        IsReady = true;
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

    private bool skipped;

    public SpeechFragment()
    {
        Text = string.Empty;
    }

    public float Play(AudioSource source)
    {
        if (skipped || Clip == null)
            return Text.Length * 0.1f;
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

    public void Skip()
    {
        skipped = true;
    }

    public bool HasClip()
    {
        return skipped || Clip != null;
    }

    public static SpeechFragment operator +(SpeechFragment fragment, string token)
    {
        fragment.Text += token;
        return fragment;
    }
}