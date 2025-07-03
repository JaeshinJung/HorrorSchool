using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Door : MonoBehaviour, IInteractable
{
    private bool isOpen = false;
    private Animator animator;

    private static readonly int IsOpenHash = Animator.StringToHash("isOpen");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.Log("Door.cs - Animator 참조 실패");
        }
    }

    // Interact 메서드에 실제 문 여닫는 로직을 구현합니다.
    public void Interact()
    {
        isOpen = !isOpen; // 열림/닫힘 상태를 토글

        // 여기에 문 여는 애니메이션이나 회전 로직을 추가하면 됩니다.
        // 예시: transform.Rotate(0, 90 * (isOpen ? 1 : -1), 0);
        animator.SetBool(IsOpenHash, isOpen);

        Debug.Log(isOpen ? $"문이 열렸습니다. {isOpen}" : $"문이 닫혔습니다.{isOpen}");
    }

    // 문을 바라봤을 때 표시될 텍스트를 상태에 맞게 변경
    public string GetInteractPrompt()
    {
        return isOpen ? "문 닫기" : "문 열기";
    }
}
