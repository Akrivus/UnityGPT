using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(SceneAgent)), RequireComponent(typeof(NavMeshAgent)), RequireComponent(typeof(Animator))]
public class WorkerAgent : MonoBehaviour
{
    private SceneAgent worker;
    private NavMeshAgent nav;
    private Animator animator;

    [SerializeField]
    private float posture = 0.0f;

    [SerializeField]
    private Stage stage;

    public float Energy
    {
        get => animator.GetFloat("Energy");
        set => animator.SetFloat("Energy", value);
    }

    public float Posture
    {
        get => animator.GetFloat("Posture");
        set => animator.SetFloat("Posture", value);
    }

    public float Mood
    {
        get => animator.GetFloat("Mood");
        set => animator.SetFloat("Mood", value);
    }

    public bool IsTalking
    {
        get => animator.GetBool("Talking");
        set => animator.SetBool("Talking", value);
    }

    public bool IsWalking
    {
        get => animator.GetBool("Walking");
        set => animator.SetBool("Walking", value);
    }

    private float tick => Time.deltaTime * (1.0f + Posture) * 0.1f;

    public void Awake()
    {
        worker = GetComponent<SceneAgent>();
        nav = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        Posture = posture;
    }

    public void Start()
    {
        worker.OnIdle += IdleState;
        worker.OnTalk += TalkState;
        worker.OnWalk += WalkState;
        worker.OnWork += WorkState;

        worker.OnMessage += Debug.Log;
    }

    public void Update()
    {
        IsTalking = worker.State == SceneAgent.SceneAgentState.Talk;
        IsWalking = worker.State == SceneAgent.SceneAgentState.Walk;

        if (!IsWalking && !IsTalking)
            nav.ResetPath();
    }

    public void IdleState(SceneAgent agent)
    {
        if (Energy >= 1f)
            agent.State = SceneAgent.SceneAgentState.Walk;
        Energy += tick;
    }

    public void TalkState(SceneAgent agent, SceneAgent other)
    {
        transform.LookAt(other.transform);
        nav.SetDestination(other.transform.position);
    }

    public void WalkState(SceneAgent agent)
    {
        if (nav.remainingDistance <= 0.1f || nav.isPathStale)
            nav.SetDestination(stage.GetRandomLocation());
        if (Energy <= -1f)
            agent.State = SceneAgent.SceneAgentState.Idle;
        Energy -= tick;
    }

    public void WorkState(SceneAgent agent)
    {

    }
}
