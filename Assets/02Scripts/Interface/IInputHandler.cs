using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInputHandler 
{
    Vector2 GetMovementInput(); // 이동
    bool IsInteractionPressed(); // 상호작용
}
