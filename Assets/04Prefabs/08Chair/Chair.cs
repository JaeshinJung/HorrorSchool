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

    private void Start()
    {
        if (demonDoll != null)
        {
            demonDoll.SetActive(false);
        }

        if (demonHead != null)
        {
            demonHead.SetActive(false);
        }

        StartCoroutine(blinkDoll());
    }

    public string GetInteractPrompt()
    {
        return hasInteracted ? "" : "\uc0b4\ud3b4\ubcf4\uae30";
    }

    public void Interact()
    {
        if (hasInteracted) return;
        
        hasInteracted = true;

        // 1. 깜빡임 중지
        StopCoroutine(blinkDoll());

        // 2. 소리재생
        audioSource.PlayOneShot(horrorSound);

        // 3. 머리 출현
        if (demonHead != null)
        {
            demonHead.SetActive(true);
        }

    }

    private IEnumerator blinkDoll()
    {
        while (!hasInteracted)
        {
            demonDoll.SetActive(true);
            yield return new WaitForSeconds(0.2f);

            demonDoll.SetActive(false);
            yield return new WaitForSeconds(1f);
        }
    }
}
