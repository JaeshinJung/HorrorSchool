using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Monitor : MonoBehaviour, IInteractable
{
    [Header("상태 관리")]
    private bool hasInteracted = false;

    [Header("UI 효과 연결")]
    public Animator ghostUIAnimator; // 귀신 이미지 UI의 애니메이터

    [Header("오디오 설정")]
    public AudioClip staticSound;               // 지지직거리는 소리 (루프)
    public AudioClip screamSound;               // 비명 소리 (일회성)

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (ghostUIAnimator != null)
        {
            ghostUIAnimator.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        audioSource.clip = staticSound;
        audioSource.loop = true;
        audioSource.Play();
    }

    public string GetInteractPrompt()
    {
        return hasInteracted ? "" : "\uc790\uc138\ud788 \ubcf8\ub2e4";
    }

    public void Interact()
    {
        if (hasInteracted) return;
        hasInteracted = true;

        // 1. 지지직 소리 멈춤
        audioSource.Stop();

        // 2. 비명소리
        audioSource.loop = false;
        audioSource.PlayOneShot(screamSound);

        // 3. 귀신 등장
        if (ghostUIAnimator != null)
        {
            ghostUIAnimator.gameObject.SetActive(true); // 비활성화된 UI를 켜고
            ghostUIAnimator.Play("Ghost_Appear");      // 애니메이션 재생
        }
    }
}
