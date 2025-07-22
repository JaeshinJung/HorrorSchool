using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

[System.Serializable]
public class Anomaly
{
    public Transform spawnPoint;      // 이상 현상이 나타날 위치 (빈 GameObject)
    public GameObject normalPrefab; // 해당 위치에 생성될 평범한 프리팹
    public GameObject anomalyPrefab;  // 해당 위치에 생성될 이상 현상 프리팹
}


public class GameManager : Singleton<GameManager>
{
    [Header("Game Objects")]
    public PlayerController playerController;
    public Transform playerStartPoint;
    public Anomaly[] anomalies; // 이상현상들

    [Header("UI References")]
    public Animator ghostUIAnimator; // 씬에 있는 Ghost UI의 애니메이터

    [Header("Game Settings")]
    public float anomalyChance = 0.5f; // 이상 현상이 나타날 확률
    public int winCondition = 8; // 탈출에 필요한 연속 정답 횟수

    [Header("UI")]
    public TextMeshPro[] streakTexts; // 연속 정답 횟수를 표시할 UI
    private bool isAnomalyActive; // 현재 방에 이상현상이 있는지 여부
    private int currentStreak = 0; // 현재 연속 정답 횟수

    //현재 생성된 모든 오브젝트(평범 + 이상) 인스턴스를 저장할 리스트
    private List<GameObject> currentRoomInstance = new List<GameObject>();

    protected override void DoAwake()
    {
        // GameManager가 시작될때 필요한 초기화 로직이 있다면 여기에 작성...
    }

    private void Start()
    {
        playerController.ResetPositionAndRotation(playerStartPoint.position, playerStartPoint.rotation);
        SetupNewRoom();
    }

    void SetupNewRoom()
    {
        // 이전 라운드에 생성된 모든 오브젝트를 파괴하여 청소
        foreach (GameObject roomInstance in currentRoomInstance) 
        {
            Destroy(roomInstance);
        }
        currentRoomInstance.Clear(); // 리스트 청소

        // 확률에 따라 이상현상 발생 결정
        isAnomalyActive = Random.value < anomalyChance;
        int anomalyIndex = -1; // -1은 이상현상 없음을 의미

        if (isAnomalyActive)
        {
            // 만약 이상현상을 발생시킨다면, 9개의 이상현상중 하나 랜덤 선택
            anomalyIndex = Random.Range(0, anomalies.Length);
            Debug.Log($"이상 현상 발생! ({anomalies[anomalyIndex].anomalyPrefab.name})");
        }
        else
        {
            Debug.Log("이상 현상 없음. 평범한 상태.");
        }

        // 모든 스폰 포인트에 오브젝트 생성
        for (int i = 0; i < anomalies.Length; i++)
        {
            Anomaly currentSpot = anomalies[i];
            GameObject prefabToSpawn;

            // 이번 순번이 이상현상이 발생할 인덱스와 같다면 이상현상 프리팹을
            // 그렇지 않다면 평범한 프리팹 생성
            if (i == anomalyIndex)
            {
                prefabToSpawn = currentSpot.anomalyPrefab;
            }
            else
            {
                prefabToSpawn = currentSpot.normalPrefab;
            }

            //선택된 프리팹을 스폰에 소환후 나중에 지울 수 있도록 리스트에 추가
            if (prefabToSpawn != null)
            {
                GameObject newInstance = Instantiate(prefabToSpawn, currentSpot.spawnPoint);
                currentRoomInstance.Add(newInstance);

                Monitor monitor = newInstance.GetComponent<Monitor>();
                if (monitor != null)
                {
                    // 이상현상이 모니터라면
                    monitor.ghostUIAnimator = this.ghostUIAnimator;
                }
            }
        }

        UpdateUI();
    }

    public void PlayerChoseDirection(ChoiceTrigger.Direction chosenDirection)
    {
        // 새로운 규칙: 이상 현상 있으면 뒤로(Backward), 없으면 앞으로(Forward)가 정답
        bool correctChoice = (isAnomalyActive && chosenDirection == ChoiceTrigger.Direction.Backward) ||
                             (!isAnomalyActive && chosenDirection == ChoiceTrigger.Direction.Forward);

        if (correctChoice)
        {
            currentStreak++;
            Debug.Log($"정답! 현재 {currentStreak}회 연속 성공");
        }
        else
        {
            currentStreak = 0;
            Debug.Log("오답! 기록이 초기화됩니다.");
        }

        if (currentStreak >= winCondition)
        {
            Debug.Log("탈출 성공! 게임 클리어!");
            this.enabled = false;
            return;
        }

        // 플레이어 위치와 시점을 시작 지점으로 리셋
        playerController.ResetPositionAndRotation(playerStartPoint.position, playerStartPoint.rotation);

        // 다음 방(루프) 준비
        SetupNewRoom();
    }

    void UpdateUI()
    {
        // 배열에 있는 모든 텍스트 오브젝트를 순회하며 내용을 변경
        foreach (var textComponent in streakTexts)
        {
            if (textComponent != null)
            {
                textComponent.text = $"1-{currentStreak + 1}";
            }
        }
    }
}


