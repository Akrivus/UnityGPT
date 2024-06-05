using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AgenticStateMachine))]
public class TalkingState : MonoBehaviour
{
    private const string INTRO_PROMPT_TEMPLATE = "{0}\n\n" +
        "You are now in a conversation with {1}." +
        "\n\n{2}\n\nLet's get started... and action!";
    private const string OUTRO_PROMPT_TEMPLATE = "OK, say goodbye to {0}.";
    private const string EXIT_CONVO_PROMPT = "\n\nIt's time to go. Use 'Exit' to finish the conversation.";

    public static int NumberOfSpeakers = 0;
    public static int MaxNumberOfSpeakers = 2;
    public static int MaxConversations = 1;

    public event Action<AgenticStateMachine, string> OnTextGenerated;
    public event Func<AgenticStateMachine, string, string> OnBeforeMessage;

    [SerializeField]
    private AgenticStateMachine ASM;

    [SerializeField]
    private AudioSource source;

    [SerializeField]
    private AgenticStateMachine guest;

    [SerializeField, Range(2, 24)]
    private int maxMessageCount = 8;

    private int messageCount = 0;

    public TalkingState Guest => guest.Talking;
    public bool HasExited { get; private set; } = true;
    public bool IsTalking { get; private set; }
    public bool IsHosting { get; private set; }

    public string Context { get; set; }

    public string LastMessage => ASM.Speaker.LastMessage;

    private void Awake()
    {
        ASM = ASM ?? GetComponent<AgenticStateMachine>();
        source = source ?? GetComponent<AudioSource>();
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
        ASM.AI.AddTool(new QuickTool(ExitConvo, "ExitConvo", "Leave for the next conversation."));
    }

    private void Exit(AgenticStateMachine asm)
    {
        ASM.AI.RemoveTool("ExitConvo");
        ASM.Speaker.ResetContext();
        messageCount = 0;
    }

    private void Tick(AgenticStateMachine asm)
    {
        ASM.transform.LookAt(guest.transform);
    }

    private void TriggerEnter(AgenticStateMachine asm, AgenticStateMachine guest)
    {
        if (!guest.CanJoin) return;

        SetGuest(guest);
        Guest.SetGuest(asm);
        Guest.Join(asm);
        Join();

        IsHosting = true;

        StartCoroutine(
            Talk(INTRO_PROMPT_TEMPLATE.Format(Context, guest.Name, guest.Description)));
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
        IsTalking = false;
        NumberOfSpeakers--;
    }

    private void LeaveBoth()
    {
        Leave();
        Guest.Leave();
    }

    private string ExitConvo(QuickTool.Args args = null)
    {
        HasExited = true;
        return OUTRO_PROMPT_TEMPLATE.Format(guest.Name);
    }

    private IEnumerator Talk(string message = null)
    {
        IsTalking = true;
        if (!HasExited)
            yield return Respond(message);
        if (!Guest.HasExited)
            yield return Guest.Respond();
        if (HasExited)
            Guest.ExitConvo();
        else if (Guest.HasExited)
            ExitConvo();
        IsTalking = false;
        if (HasExited && Guest.HasExited)
            LeaveBoth();
        else yield return Talk();
    }

    private IEnumerator Respond(string message = null)
    {
        if (message == null) message = Guest.LastMessage;
        message = OnBeforeMessage?.Invoke(ASM, message) ?? message;
        if (messageCount >= maxMessageCount)
            message += EXIT_CONVO_PROMPT;
        yield return RespondTo(message, guest.Name);
    }

    private IEnumerator RespondTo(string message, string name)
    {
        yield return ASM.Speaker.RespondTo(message, name, this.name).Then((value) => OnTextGenerated(ASM, value)).Then(() => messageCount++);
        yield return ASM.Speaker.PlaySpeech(source);
        yield return new WaitFor(() => ASM.Speaker.IsReady, ASM.Speaker.MaxResponseTimeInMS);
        
        if (ASM.Speaker.IsReady)
            yield return null;
        ASM.Speaker.SetReady();
    }
}
