using UnityEngine;

public class WaitingState : MonoBehaviour
{
    [SerializeField]
    private AgenticStateMachine ASM;

    [SerializeField, Range(0, 2)]
    private float burnRate;

    private float minEnergy = 0;
    private float maxEnergy = 1;

    public float Energy { get; private set; }
    public float BurnRate => burnRate;

    private void Awake()
    {
        ASM = ASM ?? GetComponent<AgenticStateMachine>();
    }

    private void Start()
    {
        ASM[AgenticState.Waiting].OnStateEnter += Enter;
        ASM[AgenticState.Waiting].OnStateTick += Tick;
        ASM[AgenticState.Waiting].OnStateExit += Exit;
    }

    private void Enter(AgenticStateMachine asm)
    {
        minEnergy = Random.Range(-1f, 0f);
        maxEnergy = Random.Range( 0f, 1f);
        Energy = Random.Range(minEnergy, maxEnergy);
    }

    private void Tick(AgenticStateMachine asm)
    {
        Energy += Time.deltaTime * burnRate;
        if (Energy > maxEnergy)
            ASM.SetState(AgenticState.Walking);
    }

    private void Exit(AgenticStateMachine asm)
    {
        ASM.Walking.Wander();
    }
}
