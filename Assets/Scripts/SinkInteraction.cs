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

    private bool isUsed = false;

    public bool TryInteract(GameObject item)
    {
        if (isUsed || item.name != "ScrewDriver")
            return false;

        isUsed = true;
        StartCoroutine(UnscrewAndDrain());
        return false; // 들고 있도록 유지
    }

    private IEnumerator UnscrewAndDrain()
    {
        float unscrewDuration = 2f;
        float drainDuration = 2f;

        // 나사 회전
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

        // 물 빠짐
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
