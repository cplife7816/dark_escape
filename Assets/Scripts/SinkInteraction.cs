using System.Collections;
using UnityEngine;

public class SinkInteraction : MonoBehaviour, IItemSocket
{
    public Transform screw;
    public Transform water;
    public Transform start;
    public Transform end;

    public AudioSource audioSource;
    public AudioClip screwSoundClip;
    public AudioClip waterDrainClip;
    public AudioClip breakSoundClip; // 💥 부서지는 소리

    private bool isUsed = false;
    private GameObject screwdriverItem;

    public bool TryInteract(GameObject item)
    {
        if (isUsed || item.name != "ScrewDriver")
            return false;

        isUsed = true;
        screwdriverItem = item;
        StartCoroutine(UnscrewAndDrainAndDestroy());
        return false; // ✅ 드라이버 유지!
    }

    private IEnumerator UnscrewAndDrainAndDestroy()
    {
        float unscrewDuration = 2f;
        float drainDuration = 2f;

        // 1. 나사 회전
        float elapsed = 0f;
        Vector3 originalRot = screw.localEulerAngles;
        Vector3 targetRot = originalRot + new Vector3(0, 0, 360);

        if (audioSource != null && screwSoundClip != null)
        {
            audioSource.clip = screwSoundClip;
            audioSource.Play();
        }

        while (elapsed < unscrewDuration)
        {
            float t = elapsed / unscrewDuration;
            screw.localEulerAngles = Vector3.Lerp(originalRot, targetRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        screw.localEulerAngles = targetRot;

        // 💥 여기서 드라이버 먼저 비활성화
        if (screwdriverItem != null)
        {
            if (audioSource != null && breakSoundClip != null)
            {
                audioSource.PlayOneShot(breakSoundClip); // 부서지는 소리 먼저 재생
            }

            screwdriverItem.SetActive(false); // 손에서 사라지게 만들기
        }

        // 2. 물 빠짐
        elapsed = 0f;
        Vector3 originalPos = water.position;
        Vector3 targetPos = new Vector3(originalPos.x, end.position.y, originalPos.z);

        Vector3 originalScale = water.localScale;
        Vector3 targetScale = originalScale * 0.5f;

        if (audioSource != null && waterDrainClip != null)
        {
            audioSource.clip = waterDrainClip;
            audioSource.Play();
        }

        while (elapsed < drainDuration)
        {
            float t = elapsed / drainDuration;
            water.position = Vector3.Lerp(originalPos, targetPos, t);
            water.localScale = Vector3.Lerp(originalScale, targetScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        water.position = targetPos;
        water.localScale = targetScale;
        water.gameObject.SetActive(false);
    }
}