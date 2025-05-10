using System.Collections;
using UnityEngine;

public class WindowInteraction : MonoBehaviour
{
    public enum HingeSide { Left, Right }

    [Header("Window Settings")]
    [SerializeField] private Transform hingeTarget;
    [SerializeField] private float openAngle = -90f;
    [SerializeField] private float animationTime = 1f;
    [SerializeField] private HingeSide hingeSide = HingeSide.Right;

    [Header("Light Settings")]
    [SerializeField] private Light pointLight;
    [SerializeField] private float pointLightIntensity = 7f;
    [SerializeField] private float maxLightRange = 5f;
    [SerializeField] private float lightDuration = 0.5f;

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;

    [Header("Lock Settings")]
    [SerializeField] public bool isLocked = false;
    [SerializeField] private AudioClip lockedSound;  // 잠김 사운드

    private bool isOpen = false;
    private bool isAnimating = false;
    private Quaternion closedRot;
    private Quaternion openRot;

    private void Start()
    {
        if (hingeTarget == null)
        {
            Debug.LogError("WindowInteraction: hingeTarget is not assigned!");
            enabled = false;
            return;
        }

        closedRot = hingeTarget.rotation;

        // 방향에 따라 회전 각도 설정
        float angle = hingeSide == HingeSide.Left ? -openAngle : openAngle;
        openRot = Quaternion.Euler(hingeTarget.eulerAngles + new Vector3(0, angle, 0));

        if (pointLight != null)
            pointLight.range = 0f;
    }

    public void Interact()
    {
        if (isAnimating) return;

        if (pointLight != null)
            TriggerLightPulse();

        if (isLocked)
        {
            if (audioSource != null && lockedSound != null)
                audioSource.PlayOneShot(lockedSound);
            return;
        }

        StartCoroutine(AnimateWindow(isOpen ? openRot : closedRot, isOpen ? closedRot : openRot));

        if (audioSource != null)
            audioSource.PlayOneShot(isOpen ? closeSound : openSound);

        isOpen = !isOpen;
    }

    private IEnumerator AnimateWindow(Quaternion from, Quaternion to)
    {
        isAnimating = true;
        float t = 0;

        while (t < animationTime)
        {
            hingeTarget.rotation = Quaternion.Slerp(from, to, t / animationTime);
            t += Time.deltaTime;
            yield return null;
        }

        hingeTarget.rotation = to;
        isAnimating = false;
    }

    private Coroutine lightCoroutine;

    private void TriggerLightPulse()
    {
        // 코루틴이 실행 중이면 중단 후 재시작
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
        float targetIntensity = pointLightIntensity;

        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = timer / halfDuration;
            pointLight.range = Mathf.Lerp(startRange, maxLightRange, t);
            pointLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);
            yield return null;
        }

        timer = 0f;

        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = timer / halfDuration;
            pointLight.range = Mathf.Lerp(maxLightRange, 0f, t);
            pointLight.intensity = Mathf.Lerp(targetIntensity, 0f, t);
            yield return null;
        }

        pointLight.range = 0f;
        pointLight.intensity = 0f;
    }

}
