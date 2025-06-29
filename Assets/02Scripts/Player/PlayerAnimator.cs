using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private Animator animator;
    private int animHash_Move = Animator.StringToHash("IsMove");

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.Log("PlayerAnimator - Awake() - animator 참조 실패");
        }
    }

    public void SetMovementAnims(bool isMove)
    {
        animator?.SetBool(animHash_Move, isMove);
    }
}
