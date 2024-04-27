using RSG;
using System;
using System.Collections;
using UnityEngine;

public class WhisperAgent : AbstractAgent
{
    public event Action<string> OnTextGenerated;

    [SerializeField]
    private TextModel model;
    [SerializeField, TextArea(2, 10)]
    private string prompt;
    [SerializeField]
    private string context;
    [SerializeField]
    private string buzzwords;
    [SerializeField]
    private int maxTokens = 1024;
    [SerializeField, Range(0.0f, 1.0f)]
    private float temperature = 0.5f;
    [SerializeField]
    private VoiceRecorder recorder;

    public WhisperTextGenerator Whisper { get; private set; }
    public TextGenerator ContextGenerator { get; private set; }

    private void Awake()
    {
        Whisper = new WhisperTextGenerator(recorder, temperature, prompt);
        ContextGenerator = new TextGenerator(prompt, model, maxTokens, temperature);
        SetNewContext(context);
    }

    public override IEnumerator RespondTo(string content, Action<string> callback)
    {
        yield return new WaitUntil(() => IsReady);
        IsReady = false;
        yield return Whisper.RespondTo(context)
            .Then(SetReady + callback);
        yield return new WaitUntil(() => IsReady);
    }

    private void SetReady(string text = "")
    {
        IsReady = true;
        OnTextGenerated?.Invoke(text);
        Whisper.AddMessage(text);
    }

    public IPromise<string> SetNewContext(string content)
        => ContextGenerator.RespondTo(content)
        .Then(context => this.context = string.Format("{0} {1}", context, buzzwords));
}
