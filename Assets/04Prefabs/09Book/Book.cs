using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Book : MonoBehaviour, IInteractable
{
    [Header("상태 관리")]
    private bool hasInteracted = false; // 상호작용을 했는지 여부

    [Header("오브젝트 및 컴포넌트 연결")]
    public GameObject particle;
    public AudioClip horrorSound;
    private AudioSource audioSource;
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        if (particle != null)
        {
            particle.SetActive(false);
        }
    }

    public void Interact()
    {
        if (hasInteracted) return;

        hasInteracted = true;

        // 1. 소리재생
        audioSource.clip = horrorSound;
        audioSource.loop = false;
        audioSource.Play();

        // 2. 효과 재생
        if (particle != null)
        { 
            particle.SetActive(true);
        }
    }

    public string GetInteractPrompt()
    {
        return hasInteracted ? "" : "살펴보기";
    }
}
