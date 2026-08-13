using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Door : MonoBehaviour, IInteractable
{
    private bool isOpen = false;
    private Animator animator;
    private AudioSource audioSource;

    [Header("오디오 클립")]
    [SerializeField] private AudioClip openSound; // [추가] 문 열리는 소리
    [SerializeField] private AudioClip closeSound; // [추가] 문 닫히는 소리

    private static readonly int IsOpenHash = Animator.StringToHash("isOpen");

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.Log("Door.cs - AudioSource \ucc38\uc870 \uc2e4\ud328");
        }
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.Log("Door.cs - Animator \ucc38\uc870 \uc2e4\ud328");
        }
    }

    // Interact 메서드에 실제 문 여닫는 로직을 구현합니다.
    public void Interact()
    {
        isOpen = !isOpen; // 열림/닫힘 상태를 토글
        animator.SetBool(IsOpenHash, isOpen);

        Debug.Log(isOpen ? $"\ubb38\uc774 \uc5f4\ub838\uc2b5\ub2c8\ub2e4. {isOpen}" : $"\ubb38\uc774 \ub2eb\ud614\uc2b5\ub2c8\ub2e4.{isOpen}");

        if (isOpen)
        {
            audioSource.PlayOneShot(openSound);
        }
        else
        {
            audioSource.PlayOneShot(closeSound);
        }
    }

    // 문을 바라봤을 때 표시될 텍스트를 상태에 맞게 변경
    public string GetInteractPrompt()
    {
        return isOpen ? "\ubb38 \ub2eb\uae30" : "\ubb38 \uc5f4\uae30";
    }

    public void Close()
    {
        // 만약 문이 열려있다면 닫습니다.
        if (isOpen)
        {
            isOpen = false;
            animator.SetBool(IsOpenHash, isOpen);
            Debug.Log("\ubb38\uc774 \uc678\ubd80\uc5d0\uc11c \ub2eb\ud614\uc2b5\ub2c8\ub2e4.");
        }
    }
}
