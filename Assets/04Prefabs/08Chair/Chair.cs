using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chair : MonoBehaviour, IInteractable
{
    [Header("상태 관리")]
    private bool hasInteracted = false; // 상호작용을 했는지 여부

    [Header("오브젝트 및 컴포넌트 연결")]
    public GameObject demonDoll;
    public GameObject demonHead;
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
        throw new System.NotImplementedException();
    }
}
