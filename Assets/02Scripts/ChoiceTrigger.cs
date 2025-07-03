using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChoiceTrigger : MonoBehaviour
{
    // 진행 방향을 구분하기 위한 열거형
    public enum Direction { Forward, Backward }

    [Tooltip("이 트리거가 '전진'인지 '후진'인지 설정합니다.")]
    [SerializeField]public Direction triggerDirection;

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어가 들어왔는지 태그로 확인
        if (other.CompareTag("Player"))
        {
            // GameManager에게 플레이어의 선택을 알림
            GameManager.Instance.PlayerChoseDirection(triggerDirection);
        }
    }
}
