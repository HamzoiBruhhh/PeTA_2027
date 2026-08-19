using UnityEngine;

public class SpeedMultiplierBehaviour : StateMachineBehaviour
{
    private static readonly int SpeedMtp = Animator.StringToHash("SpeedMultiplier");
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int Duration = Animator.StringToHash("Duration");

    public float minDuration;
    public float maxDuration;

    private float speedOri;

    public override void OnStateEnter (Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        
    }

    public override void OnStateExit (Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        
    }
}