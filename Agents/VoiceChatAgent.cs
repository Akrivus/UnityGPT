using System.Diagnostics;
using UnityEngine;

public class VoiceChatAgent : MonoBehaviour
{
    [SerializeField] VoiceRecorder recorder;
    [SerializeField] ChatGenerator chat;
    [SerializeField] string prompt;
    [SerializeField, Range(0.0f, 1.0f)] float temperature = 0.5f;

    SpeechToTextGenerator generator;
    Stopwatch timer = new Stopwatch();

    public ChatGenerator ChatGenerator => chat;
    public SpeechToTextGenerator SpeechToTextGenerator => generator;

    public void Activate()
    {
        StartCoroutine(generator.GenerateText(prompt));
    }

    void Awake()
    {
        generator = new SpeechToTextGenerator(recorder, prompt, SpeechToTextModel.Whisper_1, temperature);
        generator.TextStart += OnTextStart;
        generator.TextComplete += OnTextComplete;
        chat.TextToSpeechComplete += OnTextToSpeechComplete;
        timer.Start();
    }

    private void OnTextStart(object sender, TextEvent e)
    {
        timer.Restart();
    }

    private void OnTextComplete(object sender, TextEvent e)
    {
        UnityEngine.Debug.Log($"Transcribed in {timer.ElapsedMilliseconds}ms");
        StartCoroutine(chat.GenerateText(e.Message));
    }

    private void OnTextToSpeechComplete(object sender, TextToSpeechEvent e)
    {
        UnityEngine.Debug.Log($"Responded after {timer.ElapsedMilliseconds}ms");
        StartCoroutine(generator.GenerateText(e.Text));
    }
}
