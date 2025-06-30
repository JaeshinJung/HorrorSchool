using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    private bool isOpen = false;

    // 인터페이스의 규칙에 따라 Interact 메서드를 구현
    public void Interact()
    {
        isOpen = !isOpen;
        Debug.Log(isOpen ? "문이 열렸습니다." : "문이 닫혔습니다.");
        // 여기에 실제로 문을 여닫는 애니메이션이나 로직을 추가하면 됩니다.
    }

    // 인터페이스의 규칙에 따라 GetInteractPrompt 메서드를 구현
    public string GetInteractPrompt()
    {
        return isOpen ? "문 닫기" : "문 열기";
    }
}
