using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MouseInput : IInputHandler
{
    public Vector2 GetMovementInput() =>
        new Vector2(Input.GetAxisRaw("Mouse X"),
        Input.GetAxisRaw("Mouse Y"));

    public bool IsInteractionPressed() => Input.GetKeyDown(KeyCode.Mouse0);
}
