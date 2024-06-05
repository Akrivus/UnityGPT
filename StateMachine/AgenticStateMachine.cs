using RSG;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class AgenticStateMachine : MonoBehaviour
{
    private const string INNER_PROMPT_TEMPLATE = "{0}\n\n" +
        "Instructions:\n{1}";
    private const string INTER_PROMPT_TEMPLATE = "{1}: {0}\n{2}: ";

    [SerializeField]
    private LinkOpenAI client;

    [SerializeField]
    private string model = "gpt-3.5-turbo";

    [SerializeField]
    private string voice = "echo";

    [SerializeField]
    private int maxTokens = 1024;

    [SerializeField]
    private float temperature = 1f;

    [SerializeField]
    private WordMapping wordMapping;

    public string Name => name;

    [TextArea(2, 5)]
    public string Description;

    [TextArea(6, 18)]
    public string Prompt;

    [TextArea(2, 12)]
    public string Instructions = "" +
        "- Speak naturally and avoid *acting out in writing.*\n" +
        "- Use simple language and avoid jargon.\n" +
        "- Remember to stay in character.\n" +
        "- Use 'Exit' when saying goodbye.\n" +
        "- Use 'Remember' to save notes.\n" +
        "- Use 'Recall' to retrieve notes.\n" +
        "- Take notes often and remember to learn.\n";

    [SerializeField]
    private Animator animator;

    [SerializeField]
    private AgenticState state;

    public Animator Animator => animator;
    public LinkOpenAI Client => client;

    public StreamingTextGenerator AI { get; private set; }
    public SpeechGenerator Speaker { get; private set; }
    public string SystemPrompt { get; private set; }

    public string LastMessage => Speaker.LastMessage;

    public WaitingState Waiting => (WaitingState) transitions[AgenticState.Waiting].state;
    public WalkingState Walking => (WalkingState) transitions[AgenticState.Walking].state;
    public TalkingState Talking => (TalkingState) transitions[AgenticState.Talking].state;
    public WorkingState Working => (WorkingState) transitions[AgenticState.Working].state;
    public EventHandler this[AgenticState state] => transitions.GetValueOrDefault(state);

    private Dictionary<AgenticState, EventHandler> transitions = new Dictionary<AgenticState, EventHandler>();

    public AgenticState State => state;
    public EventHandler Current => transitions[state];

    public bool CanJoin => TalkingState.NumberOfSpeakers / 2 < TalkingState.MaxConversations
        && TalkingState.NumberOfSpeakers % 2 == 0
        && Is(AgenticState.Walking) && Speaker.IsReady;

    private void Awake()
    {
        SystemPrompt = INNER_PROMPT_TEMPLATE.Format(Prompt, Instructions);
        AI = new StreamingTextGenerator(client, SystemPrompt, model, maxTokens, temperature, INTER_PROMPT_TEMPLATE);
        Speaker = new SpeechGenerator(client, AI, wordMapping, voice, Roles.Assistant);

        animator = animator ?? GetComponent<Animator>();

        transitions[AgenticState.Waiting] = new EventHandler(GetComponent<WaitingState>());
        transitions[AgenticState.Walking] = new EventHandler(GetComponent<WalkingState>());
        transitions[AgenticState.Talking] = new EventHandler(GetComponent<TalkingState>());
        transitions[AgenticState.Working] = new EventHandler(GetComponent<WorkingState>());
    }

    private void Start()
    {
        Current.Enter(this);
    }

    private void Update()
    {
        Current.Tick(this);
    }

    private void OnTriggerEnter(Collider collider)
    {
        var asm = collider.GetComponent<AgenticStateMachine>();
        if (asm == null) return;

        Current.TriggerEnter(this, asm);
    }

    private void OnTriggerExit(Collider collider)
    {
        var asm = collider.GetComponent<AgenticStateMachine>();
        if (asm == null) return;

        Current.TriggerExit(this, asm);
    }

    public void SetState(AgenticState state)
    {
        if (this.state == state) return;
        Current.Exit(this);
        this.state = state;
        Current.Enter(this);
    }

    public bool Is(AgenticState state)
        => this.state == state;

    public class EventHandler
    {
        public event Action<AgenticStateMachine> OnStateEnter;
        public event Action<AgenticStateMachine> OnStateExit;
        public event Action<AgenticStateMachine> OnStateTick;
        public event Action<AgenticStateMachine, AgenticStateMachine> OnTriggerEnter;
        public event Action<AgenticStateMachine, AgenticStateMachine> OnTriggerExit;

        protected internal MonoBehaviour state { get; private set; }

        public EventHandler(MonoBehaviour state)
        {
            this.state = state;
        }

        public void Enter(AgenticStateMachine asm)
        {
            OnStateEnter?.Invoke(asm);
        }

        public void Exit(AgenticStateMachine asm)
        {
            OnStateExit?.Invoke(asm);
        }

        public void Tick(AgenticStateMachine asm)
        {
            OnStateTick?.Invoke(asm);
        }

        public void TriggerEnter(AgenticStateMachine asm, AgenticStateMachine other)
        {
            OnTriggerEnter?.Invoke(asm, other);
        }

        public void TriggerExit(AgenticStateMachine asm, AgenticStateMachine other)
        {
            OnTriggerExit?.Invoke(asm, other);
        }
    }
}

public enum AgenticState
{
    Waiting,
    Walking,
    Talking,
    Working,
}