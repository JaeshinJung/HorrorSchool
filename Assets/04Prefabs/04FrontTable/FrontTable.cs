using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrontTable : MonoBehaviour, IInteractable
{
    [Header("상태 관리")]
    private bool hasInteracted = false;

    [Header("오브젝트 및 컴포넌트 연결")]
    public Renderer blinkingStainRenderer;  // 깜빡이는 핏자국의 렌더러
    public GameObject permanentStainObject; // 상호작용 후 나타날 핏자국
    public GameObject dollObject;           // 상호작용 후 나타날 인형
    public AudioClip dollExist;

    [Header("페이드 효과 설정")]
    public float fadeDuration = 1.0f;     // 나타나고 사라지는 데 걸리는 시간
    public float visibleDuration = 0.5f;  // 완전히 나타나 있는 시간
    public float invisibleDuration = 1.0f; // 완전히 사라져 있는 시간

    private AudioSource audioSource;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }
    private void Start()
    {
        // 효과 오브젝트 비활성화
        if(permanentStainObject != null)
            permanentStainObject.SetActive(false);
        if(dollObject != null)
            dollObject.SetActive(false);

        fadeCoroutine = StartCoroutine(FadeBlood());
    }

    public string GetInteractPrompt()
    {
        return hasInteracted ? "" : "\uc0b4\ud3b4\ubcf4\uae30";
    }

    public void Interact()
    {
        if (hasInteracted) return;
        hasInteracted = true;

        // 1. 기존의 깜빡이는 효과를 멈추고 숨김
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        blinkingStainRenderer.gameObject.SetActive(false);

        // 2. 영구적인 핏자국과 인형을 나타나게 함
        if (permanentStainObject != null) permanentStainObject.SetActive(true);
        if (dollObject != null) dollObject.SetActive(true);

        // 여기에 '펑' 하는 사운드를 추가하면 효과가 더 좋습니다.
        audioSource.PlayOneShot(dollExist);
    }

    private IEnumerator FadeBlood()
    {
        SetMaterialAlpha(0);

        while (!hasInteracted)
        {
            yield return new WaitForSeconds(invisibleDuration);

            // 서서히 나타나게하기
            for (float t = 0; t < fadeDuration; t += Time.deltaTime)
            {
                SetMaterialAlpha(t / fadeDuration);
                yield return null;
            }

            SetMaterialAlpha(1);

            yield return new WaitForSeconds(visibleDuration);

            // 서서히 사라지기
            for (float t = 0; t < fadeDuration; t += Time.deltaTime)
            {
                SetMaterialAlpha(1 - (t / fadeDuration));
                yield return null;
            }
            SetMaterialAlpha(0);
        }
    }

    private void SetMaterialAlpha(float alpha)
    {
        Color color = blinkingStainRenderer.material.color;
        color.a = alpha;
        blinkingStainRenderer.material.color = color;
    }
}
