using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyboardInput : IInputHandler
{
    public Vector2 GetMovementInput() => 
        new Vector2(Input.GetAxisRaw("Horizontal"),
        Input.GetAxisRaw("Vertical"));

    public bool IsInteractionPressed() => Input.GetKeyDown(KeyCode.F);
}
