using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chat : MonoBehaviour
{
    [TextArea(3, 10)]
    [SerializeField] string prompt;
    [SerializeField] ChatAgent agent;
    [SerializeField] ChatAgent guest;
    bool isExited;
    
    List<string> messages;

    public virtual string Prompt
    {
        get => prompt;
        set => prompt = value;
    }

    public virtual bool IsExited
    {
        get => isExited;
        set => isExited = value;
    }

    void Start()
    {
        
    }

    void Update()
    {

    }

    void OnTriggerEnter(Collider other)
    {
        var agent = other.GetComponent<ChatAgent>();
        if (agent == null) return;
        if (guest == null)
            Enter(agent, prompt);
    }

    public void Enter(ChatAgent agent, string prompt)
    {
        if (guest == agent)
            return;
        if (guest != null)
            Exit(guest);
        guest = agent;
        guest.ChatComplete += OnGuestComplete;
        guest.GenerateText(prompt);
    }

    public void Exit(ChatAgent agent)
    {
        if (guest != agent)
            return;
        guest.ChatComplete -= OnGuestComplete;
        guest.ClearMessages();
        agent.ClearMessages();
        guest = null;
    }

    private void OnGuestComplete(object sender, ChatEventArgs e)
    {
        messages.Add($"{guest.name}: {e.Message}");
    }

    private void OnAgentComplete(object sender, ChatEventArgs e)
    {
        messages.Add($"{agent.name}: {e.Message}");
    }
}
