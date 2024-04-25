using RSG;
using System;
using System.Collections;
using UnityEngine;

public class WhisperAgent : AbstractAgent
{
    public event Action<string> OnTextGenerated;

    [SerializeField, TextArea(2, 10)]
    private string prompt;
    [SerializeField]
    private string context;
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
        ContextGenerator = new TextGenerator(prompt, TextModel.GPT_3_Turbo, maxTokens, temperature);
        ContextGenerator.OnTextGenerated += SetNewContext;
    }

    public override IEnumerator RespondTo(string content, Action<string> callback)
    {
        ContextGenerator.RespondTo(content);

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

    private IPromise<string> SetNewContext(string context)
        => Promise<string>.Resolved(this.context = context);
}
