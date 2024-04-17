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
    bool isReset;

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

    public void ResetChat()
    {
        isReset = true;
    }

    private IPromise<string> PlayChatTurn(string message, int index)
    {
        if (isReset)
        {
            foreach (var agent in agents)
                agent.ResetContext();
            isReset = false;
            return PlayChatTurn(context, 0);
        }
        return agents[index].Ask(message).Then((text) => PlayChatTurn(text, (index + 1) % agents.Length));
    }

    private IEnumerator StartChat()
    {
        yield return new WaitUntil(() => !recorder.IsCalibrating);
        yield return PlayChatTurn(context, 0);
    }
}
