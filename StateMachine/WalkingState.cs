using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(AgenticStateMachine)), RequireComponent(typeof(NavMeshAgent))]
public class WalkingState : MonoBehaviour
{
    [SerializeField]
    private AgenticStateMachine ASM;

    [SerializeField]
    private NavMeshAgent agent;

    [SerializeField, Range(1, 120)]
    private float delay;
    private float timer;

    [SerializeField]
    private Transform[] circuit;
    private int i;

    public Transform Target;

    public bool IsWalking
    {
        get => ASM.Animator.GetBool("Walking");
        set => ASM.Animator.SetBool("Walking", value);
    }

    private void Awake()
    {
        ASM = ASM ?? GetComponent<AgenticStateMachine>();
        agent = agent ?? GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        ASM[AgenticState.Walking].OnStateEnter += Enter;
        ASM[AgenticState.Walking].OnStateTick += Tick;
        ASM[AgenticState.Walking].OnStateExit += Exit;

        ASM[AgenticState.Walking].OnTriggerEnter += (a, b) => Wander();
    }

    private void Enter(AgenticStateMachine asm)
    {
        IsWalking = true;
        agent.radius = 0.5f;
        agent.SetDestination(Target.position);
        agent.isStopped = false;
    }

    private void Tick(AgenticStateMachine asm)
    {
        timer += Time.deltaTime;
        if (agent.remainingDistance < timer || agent.isPathStale || timer > delay
            || (timer > 1f && 1f > agent.velocity.magnitude / agent.speed))
        {
            ASM.SetState(AgenticState.Waiting);
            if (circuit.Length == 0) return;
            i = (i + 1) % circuit.Length;
            Target.position = circuit[i].position;
        }
    }

    private void Exit(AgenticStateMachine asm)
    {
        IsWalking = false;
        agent.radius = 0.3f;
        agent.isStopped = true;
        timer = 0;
    }

    public void Wander()
    {
        Target.transform.localPosition = RandomDonut(1, 4);
    }

    private Vector3 RandomDonut(float minRadius, float maxRadius)
    {
        return new Vector3(RandomDonutShape(minRadius, maxRadius), 0, RandomDonutShape(minRadius, maxRadius));
    }

    private float RandomDonutShape(float minRadius, float maxRadius)
    {
        var probe = Random.Range(minRadius, maxRadius);
        var truey = Random.Range(0, 1) > 0.5f;
        return truey ? probe : -probe;
    }
}
