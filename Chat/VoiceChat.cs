using System.Diagnostics;
using UnityEngine;

public class VoiceChat : MonoBehaviour
{
    [SerializeField] ChatAgent agent;
    [SerializeField] VoiceRecorder recorder;
    [SerializeField] string prompt;
    [SerializeField, Range(0.0f, 1.0f)] float temperature = 0.5f;

    SpeechToTextGenerator asr;
    Stopwatch timer = new Stopwatch();

    public void Activate()
    {
        StartCoroutine(asr.GenerateText(prompt));
    }

    void Awake()
    {
        asr = new SpeechToTextGenerator(recorder, prompt, SpeechToTextModel.Whisper_1, temperature);
        asr.TextStart += OnTextStart;
        asr.TextComplete += OnTextComplete;
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
        StartCoroutine(asr.GenerateText(e.Text));
    }
}
