using System.Diagnostics;
using UnityEngine;

public class WhisperChat : MonoBehaviour
{
    [SerializeField] ChatAgent agent;
    [SerializeField] VoiceRecorder recorder;
    [SerializeField] string prompt;
    [SerializeField, Range(0.0f, 1.0f)] float temperature = 0.5f;

    WhisperTextGenerator whisper;
    Stopwatch timer = new Stopwatch();

    public ChatAgent Agent => agent;
    public WhisperTextGenerator Whisper => whisper;

    public void Activate()
    {
        StartCoroutine(whisper.GenerateText(prompt));
    }

    void Awake()
    {
        whisper = new WhisperTextGenerator(recorder, prompt, temperature);
        whisper.TextStart += OnTextStart;
        whisper.TextComplete += OnTextComplete;
        agent.TextToSpeechComplete += OnTextToSpeechComplete;
        timer.Start();
    }

    private void OnTextStart(object sender, TextEventArgs e)
    {
        timer.Restart();
    }

    private void OnTextComplete(object sender, TextEventArgs e)
    {
        UnityEngine.Debug.Log($"Transcribed in {timer.ElapsedMilliseconds}ms");
        StartCoroutine(agent.GenerateText(e.Message));
    }

    private void OnTextToSpeechComplete(object sender, TextToSpeechEventArgs e)
    {
        UnityEngine.Debug.Log($"Responded after {timer.ElapsedMilliseconds}ms");
        StartCoroutine(whisper.GenerateText(e.Text));
    }
}
