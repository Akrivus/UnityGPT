using RSG;
using System.Collections;
using UnityEngine;

public class MultiAgentChat : MonoBehaviour
{
    [SerializeField] TextToSpeechAgent[] agents;
    [SerializeField] string message;

    void Start()
    {
        StartCoroutine(StartChat());
    }

    private IPromise<string> PlayChatTurn(string message, int index)
    {
        return agents[index].Ask(message).Then((text) => PlayChatTurn(text, (index + 1) % agents.Length));
    }

    private IEnumerator StartChat()
    {
        yield return PlayChatTurn(message, 0);
    }
}
