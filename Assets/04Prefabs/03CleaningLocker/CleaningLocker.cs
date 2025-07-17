using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CleaningLocker : MonoBehaviour, IInteractable
{
    [Header("상태 관리")]
    private bool hasInteracted = false; // 상호작용을 했는지 여부

    [Header("오브젝트 및 컴포넌트 연결")]
    public ParticleSystem bloodEffect;    // 피가 흘러나오는 파티클 시스템
    public GameObject bloodPrint;
    public AudioClip bangingSound;        // 쾅쾅거리는 소리 (루프용)
    public AudioClip screamSound;         // 비명소리 (일회성)
    private AudioSource audioSource;
    private Animator animator;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (bloodEffect != null)
        {
            bloodEffect.gameObject.SetActive(false);
        }

        if (bloodPrint != null)
        {
            bloodPrint.gameObject.SetActive(false);
        }

        audioSource.clip = bangingSound;
        audioSource.loop = true;
        audioSource.Play();
    }

    public string GetInteractPrompt()
    {
        return hasInteracted ? "" : "F로 조사하기";
    }

    public void Interact()
    {
        if (hasInteracted) return;

        hasInteracted = true;

        // 1. 기존 소리와 떨림을 멈춤
        animator.SetBool("hasInteracted", true);
        audioSource.Stop();


        // 2. 비명소리 재생
        audioSource.loop = false;
        audioSource.PlayOneShot(screamSound);

        // 3. 피 효과 활성화 및 재생
        if (bloodEffect != null)
        {
            bloodEffect.gameObject.SetActive(true);
            bloodEffect.Play();
        }

        if (bloodPrint != null)
        {
            bloodPrint.SetActive(true);
        }
    }
}
