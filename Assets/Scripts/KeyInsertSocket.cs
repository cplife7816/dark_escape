using System.Collections;
using UnityEngine;

public class KeyInsertSocket : MonoBehaviour, IItemSocket
{
    [Header("Required Key Name")]
    [SerializeField] private string requiredKeyName;

    [Header("Object to Activate")]
    [SerializeField] private GameObject keyVisualObject;

    [Header("Sound Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip insertSound;

    [Header("Light Settings")]
    [SerializeField] private Light pointLight;
    [SerializeField] private float lightRange = 5f;
    [SerializeField] private float lightIntensity = 7f;
    [SerializeField] private float lightDuration = 0.5f;

    private bool isUsed = false;
    private Coroutine lightCoroutine;

    public bool TryInteract(GameObject item)
    {
        if (isUsed || item == null || item.name != requiredKeyName)
            return false;

        Debug.Log($"[KeyInsertSocket] 올바른 열쇠({item.name}) 감지됨 → 삽입 처리");

        isUsed = true;

        // 🔊 소리
        if (audioSource != null && insertSound != null)
        {
            audioSource.PlayOneShot(insertSound);
        }

        // 💡 빛 효과
        if (pointLight != null)
        {
            TriggerLocalLightPulse();
        }

        // 🔑 비주얼 키 삽입 표현
        if (keyVisualObject != null)
            keyVisualObject.SetActive(true);

        // 🧤 아이템 비활성화
        item.SetActive(false);

        return true;
    }

    private void TriggerLocalLightPulse()
    {
        if (lightCoroutine != null)
            StopCoroutine(lightCoroutine);

        lightCoroutine = StartCoroutine(PulseLightEffect());
    }

    private IEnumerator PulseLightEffect()
    {
        float halfDuration = lightDuration / 2f;
        float timer = 0f;

        float startRange = pointLight.range;
        float startIntensity = pointLight.intensity;

        // 증가 구간
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = timer / halfDuration;
            pointLight.range = Mathf.Lerp(startRange, lightRange, t);
            pointLight.intensity = Mathf.Lerp(startIntensity, lightIntensity, t);
            yield return null;
        }

        // 감소 구간
        timer = 0f;
        startRange = pointLight.range;
        startIntensity = pointLight.intensity;

        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = timer / halfDuration;
            pointLight.range = Mathf.Lerp(startRange, 0f, t);
            pointLight.intensity = Mathf.Lerp(startIntensity, 0f, t);
            yield return null;
        }

        pointLight.range = 0f;
        pointLight.intensity = 0f;
    }

    public bool CanInteract(GameObject item)
    {
        return !isUsed && item != null && item.name == requiredKeyName;
    }
}
