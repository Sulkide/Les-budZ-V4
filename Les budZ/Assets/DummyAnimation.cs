using Unity.VisualScripting;
using UnityEngine;

public class DummyAnimation : MonoBehaviour
{
    public Animator animator;  
    void Start()
    {
        animator = gameObject.transform.GetChild(0).GetComponent<Animator>();
        Idle();
    }


    public void Idle()
    {
        animator.SetBool("isWalking",false);
        animator.SetBool("isRunning",false);
        animator.SetBool("isJumping",false);
        animator.SetBool("isFalling",false);
        animator.SetBool("isSliding",false);
        animator.SetBool("isDamageUp",false);
        animator.SetBool("isDashing",false);
        animator.SetBool("isCAC",false);
        animator.SetBool("isPushing",false);
        animator.SetBool("isSlidingDown",false);
        
        animator.SetBool("isTalkingNormal",false);
        animator.SetBool("isTalkingStress",false);
        animator.SetBool("isTalkingSad",false);
        animator.SetBool("isTalkingHappy",false);
        animator.SetBool("isTalkingAngry",false);
        animator.SetBool("isShocked",false);
        animator.SetBool("isGiving",false);
    }

    public void TalkingNormal()
    {
        animator.SetBool("isTalkingNormal",true);
    }

    public void TalkingStress()
    {
        animator.SetBool("isTalkingStress",true);
    }

    public void TalkingSad()
    {
        animator.SetBool("isTalkingSad",true);
    }

    public void TalkingHappy()
    {
        animator.SetBool("isTalkingHappy",true);
    }

    public void TalkingAngry()
    {
        animator.SetBool("isTalkingAngry",true);
    }

    public void Shocked()
    {
        animator.SetBool("isShocked",true);
    }

    public void Giving()
    {
        animator.SetBool("isGiving",true);
    }
}
