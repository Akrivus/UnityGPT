using RSG;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(AgenticStateMachine))]
public class TalkingState : MonoBehaviour
{
    private const string INTRO_PROMPT_TEMPLATE = "Context:\n{0}\n\n" +
        "You are now in a conversation with {1}.\n\n" +
        "Let's get started... and action!";
    private const string OUTRO_PROMPT_TEMPLATE = "Leaving; wrap up the conversation with {0}.";
    private const string EXIT_CONVO_PROMPT = "\n\n(You have reached the maximum number of messages.)";

    public static int NumberOfSpeakers = 0;
    public static int MaxNumberOfSpeakers = 2;
    public static int MaxConversations = 1;

    public event Action<AgenticStateMachine, string> OnBeforeMessage;
    public event Action<AgenticStateMachine, AgenticStateMachine, List<string>> OnBeforeContext;
    public event Action<AgenticStateMachine, string> OnTextGenerated;

    [SerializeField]
    private AgenticStateMachine ASM;

    [SerializeField]
    private NavMeshAgent agent;

    [SerializeField]
    private AudioSource source;

    [SerializeField]
    private AgenticStateMachine guest;

    [SerializeField, Range(2, 6)]
    private int guestRepeatRate = 4;
    private string[] pastGuests;

    [SerializeField, Range(2, 24)]
    private int maxMessageCount = 8;
    private int messageCount = 0;

    [SerializeField, TextArea(2, 5)]
    private string context;
    private List<string> contexts;

    public AgenticStateMachine Guest => guest;
    public bool IsTalking => source.isPlaying;
    public bool IsHosting { get; private set; }
    public bool HasExited { get; private set; } = true;

    public string LastMessage => ASM.Speaker.LastMessage;

    private void Awake()
    {
        ASM = ASM ?? GetComponent<AgenticStateMachine>();
        agent = agent ?? GetComponent<NavMeshAgent>();
        source = source ?? GetComponent<AudioSource>();
        pastGuests = new string[guestRepeatRate];
    }

    private void Start()
    {
        ASM[AgenticState.Talking].OnStateEnter += Enter;
        ASM[AgenticState.Talking].OnStateExit += Exit;
        ASM[AgenticState.Talking].OnStateTick += Tick;
        ASM[AgenticState.Waiting].OnTriggerEnter += TriggerEnter;
    }
    
    private void Enter(AgenticStateMachine asm)
    {
        agent.destination = ASM.transform.position;
        agent.isStopped = false;
        ASM.AI.AddTool(new QuickTool(LeaveConvo, "LeaveConvo", "Leave the conversation."));
    }

    private void Exit(AgenticStateMachine asm)
    {
        for (int i = 0; i < guestRepeatRate; i++)
            pastGuests[i] = i < guestRepeatRate - 1 ? pastGuests[i + 1] : guest.Name;
        messageCount = 0;
        agent.isStopped = false;
        ASM.AI.RemoveTool("LeaveConvo");
        ASM.Speaker.ResetContext();
    }

    private void Tick(AgenticStateMachine asm)
    {
        ASM.transform.LookAt(guest.transform);
    }

    private void TriggerEnter(AgenticStateMachine asm, AgenticStateMachine guest)
    {
        if (!guest.CanJoin) return;

        // Prevent repeat conversations with the same guest.
        foreach (var name in pastGuests)
            if (name == guest.Name)
                return;

        SetGuest(guest);
        Guest.Talking.SetGuest(asm);
        Guest.Talking.Join(asm);
        Join();

        IsHosting = true;

        contexts = new List<string>() { asm.Description };
        if (OnBeforeContext != null)
            OnBeforeContext(ASM, Guest, contexts);
        context = string.Join("\n", contexts);
        StartCoroutine(Talk(INTRO_PROMPT_TEMPLATE.Format(context, name)));
    }

    private void SetGuest(AgenticStateMachine asm)
    {
        asm.Speaker.TextOnly = NumberOfSpeakers >= MaxNumberOfSpeakers;
        guest = asm;
        HasExited = false;
        NumberOfSpeakers++;
    }

    private void Join(AgenticStateMachine asm = null)
    {
        IsHosting = false;
        ASM.SetState(AgenticState.Talking);
    }

    private void Leave()
    {
        ASM.Walking.Wander();
        ASM.SetState(AgenticState.Walking);
        NumberOfSpeakers--;
    }

    private void LeaveBoth()
    {
        Leave();
        Guest.Talking.Leave();
    }

    private string LeaveConvo(QuickTool.Args args = null)
    {
        HasExited = true;
        return OUTRO_PROMPT_TEMPLATE.Format(guest.Name);
    }

    private IEnumerator Talk(string message = null)
    {
        if (!HasExited)
            yield return Respond(message);
        if (!Guest.Talking.HasExited)
            yield return Guest.Talking.Respond();
        if (HasExited)
            Guest.Talking.LeaveConvo();
        else if (Guest.Talking.HasExited)
            LeaveConvo();
        if (HasExited && Guest.Talking.HasExited)
            LeaveBoth();
        else yield return Talk();
    }

    private IEnumerator Respond(string message = null)
    {
        // Respond to the last message if no message is provided.
        if (message == null) message = Guest.LastMessage;
        if (OnBeforeMessage != null)
            OnBeforeMessage(ASM, message);
        yield return RespondTo(message, ASM.Name);
    }

    private IEnumerator RespondTo(string message, string name)
    {
        // Nudge GPT if things are taking too long.
        if (messageCount >= maxMessageCount)
            message += EXIT_CONVO_PROMPT;

        yield return ASM.Speaker.RespondTo(message, name, ASM.Name)
            .Then((value) => OnTextGenerated(ASM, value))
            .Then(() => messageCount++);
        yield return ASM.Speaker.PlaySpeech(source);
        yield return new WaitFor(() => ASM.Speaker.IsReady, ASM.Speaker.MaxResponseTimeInMS);
        
        // If the speaker still isn't ready after ~30s, force it to be ready.
        if (ASM.Speaker.IsReady)
            yield return null;
        ASM.Speaker.SetReady();
    }
}
