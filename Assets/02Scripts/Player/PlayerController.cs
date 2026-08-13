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

    // 사운드 세팅
    [Header("Audio Setting")]
    [SerializeField] private AudioClip footstepSound;
    private AudioSource audioSource;
    [SerializeField] private float footstepInterval = 0.5f;
    private float footstepTime = 0f;

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
    private bool isViewInitialized = false;
    private IInteractable currentInteractable; // 현재 바라보고 있는 상호작용 오브젝트
    private Vector3 hitNormal;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            Debug.Log("Debug Log - PlayerController.cs / Awake() / controller \ucc38\uc870 \uc2e4\ud328");
        }
        animator = GetComponent<PlayerAnimator>();
        if (animator == null)
        {
            Debug.Log("Debug Log - PlayerController.cs / Awake() / animator \ucc38\uc870 \uc2e4\ud328");
        }

        keyboardHandler = new KeyboardInput();
        if (keyboardHandler == null)
        {
            Debug.Log("Debug Log - PlayerController.cs / Awake() / keyboardHandler \ucc38\uc870 \uc2e4\ud328");
        }

        mouseHandler = new MouseInput();
        if (mouseHandler == null)
        {
            Debug.Log("Debug Log - PlayerController.cs / Awake() / mouseHandler \ucc38\uc870 \uc2e4\ud328");
        }

        // 상호작용 UI꺼져있는지 확인
        if (interactionPromptUI != null)
            interactionPromptUI.gameObject.SetActive(false);

        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        StartCoroutine(InitializeViewRoutine());

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private IEnumerator InitializeViewRoutine()
    {
        yield return new WaitForEndOfFrame();

        isViewInitialized = true;
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
        Vector3 move = transform.right * keyInput.x + transform.forward * keyInput.y;
      
        // 사운드 추가
        if (controller.isGrounded && move.magnitude > 0.1f)
        {
            footstepTime -= Time.deltaTime;
            if (footstepTime <= 0)
            {
                PlayFootstepSound();
                footstepTime = footstepInterval;
            }
        }

        if (Vector3.Dot(move, hitNormal) < 0)
        {
            move = move - hitNormal * Vector3.Dot(move, hitNormal);
        }

        controller.Move(move * moveSpeed * Time.deltaTime);

        if (animator != null)
        {
            animator.SetMovementAnims(move.magnitude > 0.1f);
        }

        // 중력 적용
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        
    }

    private void HandleLook()
    {
        if (!isViewInitialized) return;

        var lookInput = mouseHandler.GetMovementInput();

        float rawMouseY = lookInput.y;

        // 좌우 회전
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up * mouseX);

        // 실제 시점 회전 계산
        float finalMouseY = rawMouseY * mouseSensitivity * Time.deltaTime;
        xRotation -= finalMouseY;
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
            if (hitInfo.collider.GetComponentInParent<IInteractable>() is IInteractable interactable)
            {
                currentInteractable = interactable;
                // UI 텍스트 설정
                interactionPromptUI.text = interactable.GetInteractPrompt();
                // UI 텍스트 활성화
                interactionPromptUI.gameObject.SetActive(true);

                // 이떄 상호작용 키가 눌렸는지 확인
                if (keyboardHandler.IsInteractionPressed() || mouseHandler.IsInteractionPressed())
                {
                    ProcessInteraction();
                }
                return;
            }
        }
        // Ray에 아무것도 맞지 않았다면 UI를 끔
        interactionPromptUI.gameObject.SetActive(false);
        currentInteractable = null;
     }

    private void ProcessInteraction()
    {
        if (currentInteractable == null) return;
        currentInteractable.Interact();
    }

    public void ResetPositionAndRotation(Vector3 postion, Quaternion rotation)
    {
        controller.enabled = false;

        transform.position = postion;
        transform.rotation = rotation;

        xRotation = 0f;
        Eyes.localRotation = Quaternion.identity;

        velocity = Vector3.zero;

        controller.enabled = true;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        hitNormal = hit.normal;
    }

    public void PlayFootstepSound()
    {
        // 소리가 단조롭지 않게 피치 변경
        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(footstepSound);
    }
}

