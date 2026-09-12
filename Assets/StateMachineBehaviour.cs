using UnityEngine;
using ithappy.Creative_Characters_FREE.Controller;
public class GrabState : StateMachineBehaviour
{
    CharacterMover mover;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        mover = animator.GetComponent<CharacterMover>();

        if (mover != null)
        {
            mover.canMove = false;
        }
    }


    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (mover != null)
        {
            mover.canMove = true;
        }
    }
}