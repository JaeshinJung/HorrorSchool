using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(CharacterController))]
// 사용자 입력 명령을 받아서
// 캐릭터를 이동시키거나 상호작용 하는 역할
public class PlayerController : MonoBehaviour
{
    // 기본 움직임 세팅
    [Header("Movement Setting")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float mouseSensitivity = 500f;
    [SerializeField] private Transform Eyes;

    // 상호작용 변수 세팅
    [Header("Interaction Setting")]
    [SerializeField] private float interactionDistance = 3f; // 상호작용 가능 거리
    [SerializeField] private LayerMask interactableLayer;    // 상호작용 가능한 레이어
    [SerializeField] private TextMeshProUGUI interactionPromptUI; // 상호작용 UI 텍스트

    // 기본 움직임 변수들
    private CharacterController controller;
    private PlayerAnimator animator;
    private IInputHandler keyboardHandler;
    private IInputHandler mouseHandler;
    private Vector3 velocity;
    private float xRotation = 0f;
    private IInteractable currentInteractable; // 현재 바라보고 있는 상호작용 오브젝트

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            Debug.Log("Debug Log - PlayerController.cs / Awake() / controller 참조 실패");
        }
        animator = GetComponent<PlayerAnimator>();
        if (animator == null)
        {
            Debug.Log("Debug Log - PlayerController.cs / Awake() / animator 참조 실패");
        }

        keyboardHandler = new KeyboardInput();
        if (keyboardHandler == null)
        {
            Debug.Log("Debug Log - PlayerController.cs / Awake() / keyboardHandler 참조 실패");
        }

        mouseHandler = new MouseInput();
        if (mouseHandler == null)
        {
            Debug.Log("Debug Log - PlayerController.cs / Awake() / mouseHandler 참조 실패");
        }

        // 상호작용 UI꺼져있는지 확인
        if (interactionPromptUI != null)
            interactionPromptUI.gameObject.SetActive(false);
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // 캐릭터 움직임
        HandleMovement();

        // 시점 변환
        HandleLook();

        // 상호작용 호출
        HandleInteraction();

    }

    private void HandleMovement()
    {
        // 캐릭터가 땅에 닿아있다면
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // 땅에 더 잘 붙어있게
        }
        var keyInput = keyboardHandler.GetMovementInput();

        // 캐릭터 이동
        Vector3 dir = transform.right * keyInput.x + transform.forward * keyInput.y;
        if (dir.magnitude >= 0.1f) // 입력값이 0이 아니라면
        {
            animator.SetMovementAnims(true);
            controller.Move(dir * (moveSpeed * Time.deltaTime));
        }
        else
        {
            animator.SetMovementAnims(false);
        }

        // 중력 적용
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleLook()
    {
        var lookInput = mouseHandler.GetMovementInput();

        // 좌우 회전
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up * mouseX);

        // 캐릭터 시점 상하 회전
        float mouseY = lookInput.y * mouseSensitivity * 1.5f * Time.deltaTime;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        Eyes.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    private void HandleInteraction()
    {
        // 카메라의 위치와 방향에서 Ray 생성
        Ray ray = new Ray(Eyes.position, Eyes.forward);

        // Raycast를 쏴서 interactabl Layer에 있는 오브젝트만 감지
        if (Physics.Raycast(ray, out RaycastHit hitInfo, interactionDistance, interactableLayer))
        {
            // Ray에 맞은 오브젝트에서 IInteractable 컴포넌트를 가져온다
            if (hitInfo.collider.TryGetComponent(out IInteractable interactable))
            {
                currentInteractable = interactable;
                // UI 텍스트 설정
                interactionPromptUI.text = interactable.GetInteractPrompt();
                // UI 텍스트 활성화
                interactionPromptUI.gameObject.SetActive(true);

                // 이떄 상호작용 키가 눌렸는지 확인
                if (keyboardHandler.IsInteractionPressed() || mouseHandler.IsInteractionPressed())
                {
                    //ProcessInteraction();
                }
                return;
            }
        }
        // Ray에 아무것도 맞지 않았다면 UI를 끔
        interactionPromptUI.gameObject.SetActive(false);
     }

    private void ProcessInteraction()
    {
        if (currentInteractable == null) return;

        // Door라면
        if (currentInteractable is Door doorComponent)
        {
            //GameManager.instance
        }
    }
}

