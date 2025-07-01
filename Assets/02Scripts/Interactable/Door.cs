using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class Door : MonoBehaviour, IInteractable
{
    public enum DoorType { Front, Back, None }

    public DoorType doorType;

    // 인터페이스의 규칙에 따라 Interact 메서드를 구현
    public void Interact(){}

    // 인터페이스의 규칙에 따라 GetInteractPrompt 메서드를 구현
    public string GetInteractPrompt()
    {
        if (doorType == DoorType.Front)
        {
            return "앞문으로 나간다";
        }
        else
        {
            return "뒷문으로 나간다";
        }
    }
}
