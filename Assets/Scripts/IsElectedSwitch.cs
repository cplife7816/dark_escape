using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// 선택: isElected를 코드로 받고 싶은 대상이 구현 (인스펙터 이벤트만 써도 됨)
public interface IElectedReceiver
{
    void SetElected(bool value);
}

/// 스위치: ITryInteractable 호환 + 레버 회전 + 사운드/빛 + isElected 브로드캐스트 + 연타 무시
public class IsElectedSwitch : MonoBehaviour, ITryInteractable
{
    [Header("Lever / Visual")]
    [SerializeField] private Transform lever;
    [SerializeField] private float offAngleX = 0f;     // 내려가 있을 때(OFF)
    [SerializeField] private float onAngleX = -30f;   // 올라가 있을 때(ON)
    [SerializeField, Min(0.05f)] private float rotateDuration = 0.18f;

    [Header("State")]
    [SerializeField] private bool isElected = false;   // 초기 상태
    public bool IsElected => isElected;

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip onClip;         // 올릴 때
    [SerializeField] private AudioClip offClip;        // 내릴 때

    [Header("Light")]
    [SerializeField] private Light pointLight;
    [SerializeField] private float targetIntensity = 2.5f;
    [SerializeField] private float targetRange = 6f;
    [SerializeField, Min(0.1f)] private float changeSpeed = 5f;

    [Header("Broadcast")]
    public UnityEvent<bool> OnElectedChanged;          // 인스펙터 이벤트(가장 쉬움)
    [SerializeField] private List<MonoBehaviour> codeReceivers = new(); // IElectedReceiver 용

    // 내부
    private Coroutine rotateCo;
    private Coroutine lightCo;
    private bool isAnimating = false;
    private Vector3 baseEuler; // Y/Z 유지용

    private void Awake()
    {
        if (!lever) lever = transform;
        baseEuler = lever.localEulerAngles;

        // 시작자세/빛 초기화
        ApplyLeverAngleImmediate(isElected ? onAngleX : offAngleX);
        if (pointLight)
        {
            pointLight.intensity = isElected ? targetIntensity : 0f;
            pointLight.range = isElected ? targetRange : 0f;
        }
    }

    private void Start()
    {
        Broadcast(isElected); // 씬 시작 시 한 번 알려주기
    }

    // ---------- ITryInteractable ----------
    public void TryInteract()
    {
        // ⚠ 연타 방지: 애니메이션 중이면 무시
        if (isAnimating) return;
        Toggle();
    }

    // ---------- 외부 제어 ----------
    public void Toggle() => SetIsElected(!isElected);

    public void SetIsElected(bool value)
    {
        // ⚠ 연타 방지: 애니메이션 중이면 무시(원하면 큐잉 로직으로 바꿀 수 있음)
        if (isAnimating) return;
        if (isElected == value) return;

        isElected = value;

        // 시각(레버) 회전
        float targetX = isElected ? onAngleX : offAngleX;
        SmoothRotateTo(targetX);

        // 사운드
        if (audioSource)
        {
            var clip = isElected ? onClip : offClip;
            if (clip) audioSource.PlayOneShot(clip);
        }

        // 빛
        if (pointLight)
        {
            if (lightCo != null) StopCoroutine(lightCo);
            lightCo = StartCoroutine(CoDriveLight(isElected));
        }

        // 전파
        Broadcast(isElected);
        Debug.Log($"[ElectedSwitch] isElected = {isElected}");
    }

    // ---------- 내부 구현 ----------
    private void SmoothRotateTo(float targetX)
    {
        if (rotateCo != null) StopCoroutine(rotateCo);
        rotateCo = StartCoroutine(RotateLeverCo(targetX, rotateDuration));
    }

    private IEnumerator RotateLeverCo(float targetX, float duration)
    {
        isAnimating = true;

        // 현재 회전을 시작점으로 사용 → 텔레포트 방지
        Quaternion fromQ = lever.localRotation;
        Quaternion toQ = Quaternion.Euler(NormalizeAngle(targetX), baseEuler.y, baseEuler.z);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float k = t;                     // 선형
            k = k * k * (3f - 2f * k);      // SmoothStep
            lever.localRotation = Quaternion.Slerp(fromQ, toQ, Mathf.Clamp01(k));
            yield return null;
        }

        lever.localRotation = toQ;
        isAnimating = false;
        rotateCo = null;
    }

    private IEnumerator CoDriveLight(bool turnOn)
    {
        float goalI = turnOn ? targetIntensity : 0f;
        float goalR = turnOn ? targetRange : 0f;

        while (pointLight &&
               (Mathf.Abs(pointLight.intensity - goalI) > 0.02f ||
                Mathf.Abs(pointLight.range - goalR) > 0.02f))
        {
            pointLight.intensity = Mathf.MoveTowards(pointLight.intensity, goalI, changeSpeed * Time.deltaTime);
            pointLight.range = Mathf.MoveTowards(pointLight.range, goalR, changeSpeed * Time.deltaTime);
            yield return null;
        }

        if (pointLight)
        {
            pointLight.intensity = goalI;
            pointLight.range = goalR;
        }
        lightCo = null;
    }

    private void ApplyLeverAngleImmediate(float x)
    {
        lever.localEulerAngles = new Vector3(NormalizeAngle(x), baseEuler.y, baseEuler.z);
    }

    private float NormalizeAngle(float a)
    {
        a %= 360f;
        if (a > 180f) a -= 360f;
        if (a < -180f) a += 360f;
        return a;
    }

    private void Broadcast(bool value)
    {
        // 1) 인스펙터 이벤트
        OnElectedChanged?.Invoke(value);

        // 2) 코드 수신자
        if (codeReceivers == null) return;
        for (int i = 0; i < codeReceivers.Count; i++)
        {
            var mb = codeReceivers[i];
            if (!mb) continue;
            if (mb is IElectedReceiver rcv) rcv.SetElected(value);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Editor ▸ Set OFF")]
    private void EditorSetOff()
    {
        isElected = false;
        ApplyLeverAngleImmediate(offAngleX);
        if (pointLight) { pointLight.intensity = 0f; pointLight.range = 0f; }
        Broadcast(isElected);
    }

    [ContextMenu("Editor ▸ Set ON")]
    private void EditorSetOn()
    {
        isElected = true;
        ApplyLeverAngleImmediate(onAngleX);
        if (pointLight) { pointLight.intensity = targetIntensity; pointLight.range = targetRange; }
        Broadcast(isElected);
    }
#endif
}
