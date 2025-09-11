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

    public bool GetLocked() => isLocked;
    public void SetLocked(bool v) { isLocked = v; }

    public float GetOpenRatio()
    {
        // 현재 회전이 닫힘/열림 중 어느 쪽에 가까운지를 0~1로 환산
        // 0 = 완전 닫힘(closedRotation), 1 = 완전 열림(openRotation)
        float total = Quaternion.Angle(closedRotation, openRotation);
        if (total <= 0.0001f) return 0f;

        float fromClosed = Quaternion.Angle(closedRotation, transform.rotation);
        float t = Mathf.Clamp01(fromClosed / total);
        return t;
    }

    // 로드 직후 '즉시' 시각을 맞추기 위해 코루틴 없이 회전값을 바로 세팅
    public void SetOpenRatioImmediate(float t01)
    {
        t01 = Mathf.Clamp01(t01);
        transform.rotation = Quaternion.Slerp(closedRotation, openRotation, t01);

        // 토글 시 튀지 않도록 내부 플래그도 일치
        // (문이 거의 열림/닫힘 임계값에 있을 땐 보수적으로 처리)
        bool nowOpen = t01 > 0.5f;
        // isOpen은 private이므로 내부 필드를 쓰고 있다면 아래와 같이 반영
        // (문 코드에서 isOpen을 private로 유지해도 무방)
        // 이 파일에서는 isOpen을 private로 들고 있으므로 반영:
        var field = typeof(Door).GetField("isOpen", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (field != null) field.SetValue(this, nowOpen);
    }

}
