using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Locker : MonoBehaviour, IInteractable
{
    [Header("상태 관리")]
    private bool hasInteracted = false; // 상호작용을 했는지 여부

    [Header("오브젝트 및 컴포넌트 연결")]
    public GameObject locker;
    public AudioClip horrorSound;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public string GetInteractPrompt()
    {
        return hasInteracted ? "" : "살펴보기";
    }

    public void Interact()
    {
        if (hasInteracted) return;
        
        hasInteracted = true;

        // 1. 소리재생
        audioSource.loop = false;
        audioSource.PlayOneShot(horrorSound);

        // 2. 락커 비활성화
        locker.SetActive(false);
    }


}
