using RSG;
using System.Collections;
using UnityEngine;

public class WhisperChat : MonoBehaviour
{
    [SerializeField] VoiceRecorder recorder;
    [SerializeField] string prompt;
    [SerializeField, Range(0.0f, 1.0f)] float temperature = 0.5f;

    [SerializeField] TextToSpeechAgent agent;

    WhisperTextGenerator whisper;

    IText[] agents;

    string context;

    public TextToSpeechAgent Agent => agent;
    public WhisperTextGenerator Whisper => whisper;

    void Awake()
    {
        whisper = new WhisperTextGenerator(recorder, prompt, temperature);
        agents = new IText[] { whisper, agent };
        context = prompt;
    }

    public void Activate()
    {
        StartCoroutine(StartChat());
    }

    private IPromise<string> PlayChatTurn(string message, int index)
    {
        return agents[index].Ask(message).Then((text) => PlayChatTurn(text, (index + 1) % agents.Length));
    }

    private IEnumerator StartChat()
    {
        yield return PlayChatTurn(context, 0);
    }
}
