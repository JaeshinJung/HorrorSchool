using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraResolution : MonoBehaviour
{
    private void Awake()
    {
        if (TryGetComponent<Camera>(out Camera mainCam))
        {
            float targetAspect = 16f / 9f; // 게임 플레이가 되어야하는 타겟 비율 16 : 9
            float screenAscpect = (float)Screen.width / Screen.height;

            float scaleHeight = screenAscpect / targetAspect;

            Rect rect = mainCam.rect;

            if (scaleHeight < 1) // 가로보다 세로가 길다 - 좌우 레터박스
            {
                rect.width = 1f;
                rect.height = scaleHeight;
                rect.x = 0f;
                rect.y = (1f - scaleHeight) / 2f;
            }
            else // 상하 레터박스
            {
                float scaleWidth = 1f / scaleHeight;
                rect.width = scaleWidth;
                rect.height = 1f;
                rect.x = (1f - scaleWidth) / 2f;
                rect.y = 0f;
            }

            mainCam.rect = rect;
        }
    }
}

