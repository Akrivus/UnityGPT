using Proyecto26;
using RSG;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SceneAgent : MonoBehaviour
{
    private const string INNER_PROMPT_TEMPLATE = "{0}\n\nSpeak plainly and respond naturally.";
    private const string INTERSTITIAL_PROMPT_TEMPLATE = "{1}: {0}\n{2}";

    private const int WORTH_TO_PRICE_RATIO = 10;

    public event Action<SceneAgent> OnIdle;
    public event Action<SceneAgent, SceneAgent> OnTalk;
    public event Action<SceneAgent> OnWalk;
    public event Action<SceneAgent> OnWork;

    public event Action<string> OnMessage;

    [SerializeField]
    private LinkOpenAI client;

    [SerializeField]
    private string model = "gpt-3.5-turbo";

    [SerializeField]
    private string voice = "echo";

    [SerializeField]
    private WordMapping wordMapping;

    [SerializeField, TextArea(3, 10)]
    private string innerPrompt;

    [SerializeField, TextArea(3, 10)]
    private string outerPrompt;

    [SerializeField]
    private List<SceneAgent> agents = new List<SceneAgent>();
    private int index = 0;

    [SerializeField, TextArea(3, 10)]
    private string message;

    [SerializeField]
    public float Budget = 1000;

    [SerializeField]
    public float Income = 0.1f;

    [SerializeField]
    public SceneAgentState State;

    private StreamingTextGenerator text;
    private SpeechGenerator speech;

    private OpinionMiner miner;

    private AudioSource source;

    public string InnerPrompt => innerPrompt;
    public string OuterPrompt => outerPrompt;
    public float Price => Budget / WORTH_TO_PRICE_RATIO;

    public bool IsReady => speech.IsReady;

    private void Awake()
    {
        var prompt = string.Format(INNER_PROMPT_TEMPLATE, innerPrompt);
        text = new StreamingTextGenerator(client, prompt, model, 1024, 1f, INTERSTITIAL_PROMPT_TEMPLATE);
        speech = new SpeechGenerator(client, text, wordMapping, voice, Roles.Assistant);

        miner = new OpinionMiner(client, prompt);

        source = GetComponent<AudioSource>();

        agents.Add(this);
        StartCoroutine(Talk());
    }

    private void Update()
    {
        Budget += Income * Time.deltaTime;

        switch (State)
        {
            case SceneAgentState.Idle:
                OnIdle?.Invoke(this);
                break;
            case SceneAgentState.Talk:
                OnTalk?.Invoke(this, agents[index]);
                break;
            case SceneAgentState.Walk:
                OnWalk?.Invoke(this);
                break;
            case SceneAgentState.Work:
                OnWork?.Invoke(this);
                break;
        }
    }

    private IEnumerator Talk()
    {
        yield return new WaitUntil(() => agents.Count > 1);

        var previous = agents[(index - 1) % agents.Count];
        yield return new WaitUntil(() => previous.IsReady);
        yield return agents[index].RespondTo(message, agents[index].name);

        index = (index + 1) % agents.Count;
        yield return Talk();
    }

    private void OnTriggerEnter(Collider other)
    {
        var agent = other.GetComponent<SceneAgent>();
        if (agent == null || agent == this) return;

        var price = agent.Price;
        var context = $"This conversation is worth {price} points. You have {Budget} points.";

        miner.Call(agent.OuterPrompt, context).Then(score => {
            var worth = UnityEngine.Random.Range(-1, 1);
            if (score < worth) return;
            Add(agent);
            Pay(agent, price);
        });
    }

    private void OnTriggerExit(Collider other)
    {
        var agent = other.GetComponent<SceneAgent>();
        if (agent == null || agent == this) return;

        Remove(agent);
    }

    private void Add(SceneAgent agent)
    {
        if (agent == null || agent == this)
            return;
        agents.Add(agent);
        message = $"{agent.name} approaches you.\nContext: {agent.OuterPrompt}";
    }

    private void Pay(SceneAgent agent, float price)
    {
        agent.Budget += price;
        Budget -= price;
    }

    private void Remove(SceneAgent agent)
    {
        if (agent == null || agent == this)
            return;
        agents.Remove(agent);
    }

    private void Exit()
    {
        agents.ForEach(agent => Remove(agent));
    }

    public IEnumerator RespondTo(string message, string name)
    {
        yield return new WaitUntil(() => speech.IsReady);
        yield return speech.RespondTo(message, name, this.name).Then(OnMessage);
        yield return speech.PlaySpeech(source);
        yield return new WaitUntil(() => speech.IsReady);
    }

    public enum SceneAgentState
    {
        Idle,
        Talk,
        Walk,
        Work,
    }
}
