using UnityEngine;

public class VoiceChatWithGPT : MonoBehaviour
{
    [SerializeField] VoiceRecorder recorder;
    [SerializeField] ChatGenerator chat;
    [SerializeField] string prompt;
    [SerializeField] string model = "whisper-1";
    [SerializeField, Range(0.0f, 1.0f)] float temperature = 0.5f;

    SpeechToTextGenerator generator;

    public ChatGenerator ChatGenerator => chat;
    public SpeechToTextGenerator SpeechToTextGenerator => generator;

    private void Start()
    {
        generator = new SpeechToTextGenerator(recorder, prompt, model, temperature);
        generator.TextComplete += OnTextComplete;
        chat.TextToSpeechComplete += OnTextToSpeechComplete;
        StartCoroutine(generator.GenerateText(prompt));
    }

    private void OnTextComplete(object sender, TextEvent e)
    {
        StartCoroutine(chat.GenerateChat(e.Message));
    }

    private void OnTextToSpeechComplete(object sender, TextToSpeechEvent e)
    {
        StartCoroutine(generator.GenerateText(e.Text));
    }
}
