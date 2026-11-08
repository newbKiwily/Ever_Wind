using UnityEngine;

public class OneShotEnd : StateMachineBehaviour
{
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool("isInteract", false);
    }
}
