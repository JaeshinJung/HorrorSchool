using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Jalousie : MonoBehaviour, IInteractable
{
    [Header("상태 관리")]
    private bool hasInteracted = false;

    [Header("오브젝트 및 컴포넌트 연결")]
    public Animator fallingPersonAnimator; // 떨어지는 사람의 애니메이터
    public AudioClip splatSound;           // 철푸덕 소리
    public GameObject bloodPrint;
    private AudioSource audioSource;

    [Header("뒤집힘 효과 설정")]
    public float flipInterval = 1.5f;     // 정상 상태로 유지되는 시간
    public float fallAnimationDuration = 0.475f; // 사람이 떨어지는 애니메이션의 길이

    private Coroutine flipCoroutine;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        if (bloodPrint != null)
        {
            bloodPrint.SetActive(false);
        }
        // 떨어지는 사람은 처음에 비활성화
        if (fallingPersonAnimator != null)
        {
            fallingPersonAnimator.gameObject.SetActive(false);
        }

        // 커튼 뒤집기 코루틴 시작
        flipCoroutine = StartCoroutine(FlipCurtain());
    }

    public string GetInteractPrompt()
    {
        return hasInteracted ? "" : "\ucc3d\ubc16\uc744 \ubcf8\ub2e4";
    }

    public void Interact()
    {
        if (hasInteracted) return;
        hasInteracted = true;

        // 1. 커튼 뒤집기 멈추고 원래대로
        if (flipCoroutine != null)
        {
            StopCoroutine(flipCoroutine);
        }
        transform.localRotation = Quaternion.identity; // 0, 0, 0 으로 회전값 초기화

        // 2. 떨어지는 사람 오브젝트 활성화 (애니메이션이 자동으로 재생됨)
        if (fallingPersonAnimator != null)
        {
            fallingPersonAnimator.gameObject.SetActive(true);
        }

        // 3. 사람이 땅에 닿을 타이밍에 맞춰 소리 재생
        StartCoroutine(PlaySplatSoundAfterDelay(fallAnimationDuration));
    }

    // 커튼을 주기적으로 뒤집는 코루틴
    private IEnumerator FlipCurtain()
    {
        while (!hasInteracted)
        {
            // 잠시 정상 상태 유지
            transform.localRotation = Quaternion.Euler(0, 0, 0);
            yield return new WaitForSeconds(flipInterval);

            // 빠르게 뒤집혔다가 돌아옴 (깜빡이는 느낌)
            transform.localRotation = Quaternion.Euler(0, 180, 0);
            yield return new WaitForSeconds(0.1f);
        }
    }

    // 지정된 시간 후에 소리를 재생하는 코루틴
    private IEnumerator PlaySplatSoundAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        audioSource.PlayOneShot(splatSound);
        if (bloodPrint != null)
        {
            bloodPrint.SetActive(true);
        }
    }
}
