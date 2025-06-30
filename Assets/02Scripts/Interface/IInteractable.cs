using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractable
{
    void Interact();

    // 상호작용시 출력할 텍스트
    string GetInteractPrompt();
}
