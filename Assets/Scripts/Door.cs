using System.Collections;
using UnityEngine;

public class Door : MonoBehaviour
{
    private bool isOpen = false;
    private bool isAnimating = false;
    private float animationTime = 1f;

    private Quaternion closedRotation;
    private Quaternion openRotation;


    public enum DoorHingeSide { Left, Right }

    [Header("Door Settings")]
    [SerializeField] private DoorHingeSide hingeSide = DoorHingeSide.Right; // 왼쪽/오른쪽 선택
    [SerializeField] private float openAngle = 90f;

    [Header("Light Settings")]
    [SerializeField] private Light pointLight;
    [SerializeField] private float pointLightIntensity = 7f;
    [SerializeField] private float maxLightRange = 5f;
    [SerializeField] private float lightDuration = 0.5f;

    [Header("Door Sounds")]
    [SerializeField] private AudioSource audioSource; // 오디오 소스
    [SerializeField] private AudioClip openSound; // 문 여는 소리
    [SerializeField] private AudioClip closeSound; // 문 닫는 소리

    [Header("Lock Settings")]
    [SerializeField] public bool isLocked = false;
    [SerializeField] private AudioClip lockedSound;


    private void Start()
    {
        closedRotation = transform.rotation;
        float angle = (hingeSide == DoorHingeSide.Right) ? openAngle : -openAngle;
        openRotation = Quaternion.Euler(transform.eulerAngles + new Vector3(0, angle, 0));
        if (pointLight != null)
        {
            pointLight.range = 0f;
        }
    }

    public void ToggleDoor()
    {
        if (isAnimating)
            return;

        if (pointLight != null)
            TriggerLightPulse();

        if (isLocked)
        {
            if (audioSource != null && lockedSound != null)
                audioSource.PlayOneShot(lockedSound);
            return;
        }

        StartCoroutine(AnimateDoor(isOpen ? openRotation : closedRotation, isOpen ? closedRotation : openRotation));

        if (audioSource != null)
            audioSource.PlayOneShot(isOpen ? closeSound : openSound);

        isOpen = !isOpen;
    }

    private IEnumerator AnimateDoor(Quaternion startRotation, Quaternion endRotation)
    {
        isAnimating = true;
        float elapsedTime = 0f;

        while (elapsedTime < animationTime)
        {
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, elapsedTime / animationTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.rotation = endRotation;
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

        // ➤ 증가
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = timer / halfDuration;
            pointLight.range = Mathf.Lerp(startRange, maxLightRange, t);
            pointLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);
            yield return null;
        }

        // ➤ 감소
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
