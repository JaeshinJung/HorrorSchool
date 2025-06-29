using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]

// 사용자 입력 명령을 받아서
// 캐릭터를 이동시키거나 상호작용 하는 역할
public class PlayerController : MonoBehaviour
{
    // 기본 움직임 세팅
    [Header("Movement Setting")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -9.81f;

    private CharacterController controller;
    private PlayerAnimator animator;
    private IInputHandler inputHandler;
    private Vector3 velocity;
    private Transform cam;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            Debug.Log("Debug Log - PlayerController.cs / Awake() / controller 참조 실패");
        }
        cam = Camera.main.transform;
        if (cam == null)
        {
            Debug.Log("Debug Log - PlayerController.cs / Awake() / cam 참조 실패");
        }
        animator = GetComponent<PlayerAnimator>();
        if (animator == null)
        {
            Debug.Log("Debug Log - PlayerController.cs / Awake() / animator 참조 실패");
        }

        inputHandler = new KeyboardInput();
        if (inputHandler == null)
        {
            Debug.Log("Debug Log - PlayerController.cs / Awake() / inputHandler 참조 실패");
        }
    }

    private void Update()
    {
        HandleMovement();

        if (inputHandler.IsInteractionPressed()) HandleInteraction();
    }

    private void HandleMovement()
    {
        var input = inputHandler.GetMovementInput();

        Vector3 moveDir = new Vector3(input.x, 0f, input.y).normalized;

        if (moveDir.magnitude >= 0.1f) // 입력값이 0이 아니라면
        {
            animator.SetMovementAnims(true);
            float angle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

            controller.Move(dir * (moveSpeed * Time.deltaTime));

            // 캐릭터의 회전 처리
            Quaternion targetRotation = Quaternion.Euler(0f, angle, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }
        else
        {
            animator.SetMovementAnims(false);
        }
    }

    private void HandleInteraction() => Debug.Log("상호작용");
}
