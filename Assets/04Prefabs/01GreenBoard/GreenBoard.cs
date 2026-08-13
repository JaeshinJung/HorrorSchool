using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GreenBoard : MonoBehaviour, IInteractable
{
    [Header("상태 관리")]
    private bool hasInteracted = false; // 상호작용을 했는지 여부

    [Header("오브젝트 및 컴포넌트 연결")]
    public TextMeshPro[] textRenderers;         // 글씨의 색을 바꿀 렌더러
    public GameObject[] bloodHandprints;     // 피 묻은 손바닥 오브젝트
    public GameObject bloodCover; // 피로 덮기
    public AudioClip bangSound;           // '쾅쾅' 소리
    private AudioSource audioSource;

    [Header("깜빡임 설정")]
    public Color normalColor = Color.white; // 평소 색
    public Color anomalyColor = Color.red;  // 깜빡일 때의 색
    public float blinkInterval = 2.0f;      // 깜빡이는 간격

    [Header("손바닥 자국 연출 설정")]
    public float initialDelay = 0.8f;   // 첫 손바닥이 찍히는 딜레이
    public float minimumDelay = 0.1f;   // 가장 빨라졌을 때의 최소 딜레이
    public float speedUpFactor = 0.75f; // 매번 딜레이가 얼마나 줄어들지 (1보다 작게)

    private Coroutine blinkCoroutine;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        // 처음엔 손바닥 자국을 숨깁니다.
        foreach (var print in bloodHandprints)
        {
            if (print != null)
            {
                print.SetActive(false);
            }
        }
        if (bloodCover != null)
        {
            bloodCover.SetActive(false);
        }

        // 글씨 깜빡임 코루틴을 시작합니다.
        blinkCoroutine = StartCoroutine(BlinkText());
    }

    public string GetInteractPrompt()
    {
        // 아직 상호작용 하지 않았다면 안내 문구 반환
        return hasInteracted ? "" : "F\ud0a4\ub85c \uc0c1\ud638\uc791\uc6a9";
    }

    public void Interact()
    {
        // 이미 상호작용했다면 아무것도 하지 않습니다.
        if (hasInteracted) return;

        // 상태 변경
        hasInteracted = true;

        // 1. 글씨 깜빡임을 멈추고 색을 빨간색으로 고정합니다.
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }

        // 상호작용 시 모든 텍스트를 빨간색으로 고정
        foreach (var renderer in textRenderers)
        {
            renderer.color = anomalyColor;
        }

        // 3. 피 묻은 손바닥 자국을 나타나게 합니다.
        StartCoroutine(ShowHandprintsSequentially());
    }

    // 글씨를 깜빡이게 하는 코루틴
    private IEnumerator BlinkText()
    {
        while (!hasInteracted)
        {
            // 배열의 모든 텍스트를 빨간색으로 변경
            foreach (var renderer in textRenderers)
            {
                renderer.color = anomalyColor;
            }
            yield return new WaitForSeconds(0.2f);

            // 배열의 모든 텍스트를 원래 색으로 복귀
            foreach (var renderer in textRenderers)
            {
                renderer.color = normalColor;
            }
            yield return new WaitForSeconds(blinkInterval);
        }
    }

    private IEnumerator ShowHandprintsSequentially()
    {
        float currentDelay = initialDelay;

        // 배열에 있는 모든 손바닥 자국을 순서대로 처리합니다.
        foreach (var handprint in bloodHandprints)
        {
            // 설정된 딜레이만큼 기다립니다.
            yield return new WaitForSeconds(currentDelay);

            // 손바닥 자국을 활성화합니다.
            if (handprint != null)
            {
                handprint.SetActive(true);
            }

            // 나타날 때마다 소리를 재생합니다.
            if (audioSource != null && bangSound != null)
            {
                audioSource.PlayOneShot(bangSound);
            }

            // 다음 딜레이를 더 짧게 만듭니다.
            currentDelay *= speedUpFactor;

            // 딜레이가 최소값보다 작아지지 않도록 제한합니다.
            if (currentDelay < minimumDelay)
            {
                currentDelay = minimumDelay;
            }
        }

        // 모든 손바닥이 찍힌 후, 마지막 연출을 위해 잠시 기다립니다.
        yield return new WaitForSeconds(1.0f);

        // 피로 덮는 오브젝트를 활성화합니다.
        if (bloodCover != null)
        {
            bloodCover.SetActive(true);
        }
        if (audioSource != null && bangSound != null)
        {
            audioSource.PlayOneShot(bangSound);
        }
    }
}