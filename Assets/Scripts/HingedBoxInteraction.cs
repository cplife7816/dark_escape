using System.Collections;
using UnityEngine;

public class HingedBoxInteraction : MonoBehaviour, ITryInteractable
{
    [Header("Lid Settings")]
    [SerializeField] private Transform lid;
    [SerializeField] private float closedAngle = -90f; // 닫힘 각도 (초기값 유지시 사용)
    [SerializeField] private float openAngle = -20f; // 열릴 때 목표 X축 각도
    [SerializeField, Min(0.1f)] private float durationSeconds = 1.0f;

    [Header("Light Settings")]
    [SerializeField] private Light pointLight;
    [SerializeField] private float targetIntensity = 2.5f;
    [SerializeField] private float targetRange = 6.0f;
    [SerializeField, Min(0.1f)] private float changeSpeed = 5f;

    [Header("Sound Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip closeClip;

    [Header("Lock Settings")]
    [SerializeField] public bool isLocked = false;     // ✅ 잠금 여부
    [SerializeField] private AudioClip lockedClip;      // (선택) 잠김 안내음

    private bool isOpen = false;
    private bool isAnimating = false;
    private Coroutine lightCo;

    private float initialX; // lid의 시작 X축 회전값

    private void Start()
    {
        if (lid == null)
        {
            Debug.LogError("[HingedBoxInteraction] lid 지정 필요!", this);
            enabled = false;
            return;
        }

        // 시작할 때 현재 X축 회전값을 저장 (예: 270)
        initialX = lid.localEulerAngles.x;

        // 빛 초기화
        if (pointLight)
        {
            pointLight.range = 0f;
            pointLight.intensity = 0f;
        }

        Debug.Log($"[HingedBoxInteraction] Init X={initialX}");
    }

    // ✅ ITryInteractable 인터페이스 구현
    public void TryInteract()
    {
        Toggle();
    }

    private void Toggle()
    {
        if (isAnimating) return;

          if (isLocked)
        {
            if (audioSource && lockedClip) audioSource.PlayOneShot(lockedClip);
            return;
        }

        float from = isOpen ? openAngle : closedAngle;
        float to = isOpen ? closedAngle : openAngle;

        StartCoroutine(CoRotate(from, to, durationSeconds));

        if (audioSource)
            audioSource.PlayOneShot(isOpen ? closeClip : openClip);

        if (pointLight)
        {
            if (lightCo != null) StopCoroutine(lightCo);
            lightCo = StartCoroutine(CoDriveLight(isOpen));
        }

        isOpen = !isOpen;
    }

    private IEnumerator CoRotate(float fromX, float toX, float seconds)
    {
        isAnimating = true;

        // 기준 X축에 더해주는 식 → Y,Z는 그대로 유지
        Quaternion fromQ = Quaternion.Euler(fromX, lid.localEulerAngles.y, lid.localEulerAngles.z);
        Quaternion toQ = Quaternion.Euler(toX, lid.localEulerAngles.y, lid.localEulerAngles.z);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / seconds;
            lid.localRotation = Quaternion.Slerp(fromQ, toQ, Mathf.Clamp01(t));
            yield return null;
        }

        lid.localRotation = toQ;
        isAnimating = false;
    }

    private IEnumerator CoDriveLight(bool wasOpen)
    {
        float goalIntensity = wasOpen ? 0f : targetIntensity;
        float goalRange = wasOpen ? 0f : targetRange;

        while (pointLight &&
               (Mathf.Abs(pointLight.intensity - goalIntensity) > 0.02f ||
                Mathf.Abs(pointLight.range - goalRange) > 0.02f))
        {
            pointLight.intensity = Mathf.MoveTowards(pointLight.intensity, goalIntensity, changeSpeed * Time.deltaTime);
            pointLight.range = Mathf.MoveTowards(pointLight.range, goalRange, changeSpeed * Time.deltaTime);
            yield return null;
        }

        if (pointLight)
        {
            pointLight.intensity = goalIntensity;
            pointLight.range = goalRange;
        }
    }
}
