using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleLookTest : MonoBehaviour
{
    public float sensitivity = 200f;
    public Transform playerBody;

    private float xRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;

        // 마우스 Y값을 직접 디버깅
        if (Input.GetAxis("Mouse Y") != 0)
        {
            Debug.Log("Raw Mouse Y Input: " + Input.GetAxis("Mouse Y"));
        }

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // 상하 회전은 카메라에 직접 적용
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        // 좌우 회전은 부모(Cube)에 적용
        playerBody.Rotate(Vector3.up * mouseX);
    }
}
