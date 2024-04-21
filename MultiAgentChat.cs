using System.Collections;
using UnityEngine;

public class MultiAgentChat : MonoBehaviour
{
    [SerializeField] string message;

    private IChatAgent[] agents;
    private int i = 0;

    public bool IsExited { get; private set; }

    private void Awake()
    {
        agents = GetComponentsInChildren<IChatAgent>();
    }

    private void Start()
    {
        StartCoroutine(Chat());
    }

    private IEnumerator Chat()
    {
        yield return new WaitUntil(() => agents[i].IsReady);
        yield return agents[i].RespondTo(message, ThenSetMessage);
        i = (i + 1) % agents.Length;
        if (!IsExited)
            yield return Chat();
    }

    private void ThenSetMessage(string response)
    {
        message = response;
    }
}
