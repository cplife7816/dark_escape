using System.Collections;
using UnityEngine;

public class Shutter : MonoBehaviour
{
    [Header("Switch")]
    [SerializeField] private SwitchButtonController switch1;

    [Header("Shrink Settings")]
    [SerializeField, Tooltip("아래에서 위로 줄어드는 데 걸리는 시간(초)")]
    private float shrinkDuration = 2f;

    [Header("Sound Effect")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip dropSound;

    [Header("Light Effect")]
    [SerializeField] private Light pointLight;
    [SerializeField] private float lightRange = 5f;
    [SerializeField] private float lightIntensity = 7f;
    [SerializeField] private float lightDuration = 0.5f;

    private bool hasShrunk = false;
    private Coroutine lightCoroutine;

    // 바닥 고정을 위한 캐시
    private Vector3 initialLocalScale;
    private Vector3 initialLocalPos;
    private Quaternion initialLocalRot;
    private float bottomLocalY;            // 로컬 메쉬 바닥(피벗 기준)
    private float bottomWorldY_Initial;    // 최초 바닥의 월드 Y

    void Awake()
    {
        initialLocalScale = transform.localScale;
        initialLocalPos = transform.localPosition;
        initialLocalRot = transform.localRotation;

        // 메시 바운즈에서 '바닥' 로컬 Y를 구함 (피벗이 중앙이어도 OK)
        if (TryGetComponent<MeshFilter>(out var mf) && mf.sharedMesh != null)
        {
            var b = mf.sharedMesh.bounds;                  // 로컬 좌표계 bounds
            bottomLocalY = b.center.y - (b.size.y * 0.5f); // 로컬 바닥
        }
        else
        {
            // 메쉬가 없으면 일단 0 가정 (필요시 빈 부모 추가 권장)
            bottomLocalY = 0f;
        }

        // 최초 바닥 월드 Y 저장
        bottomWorldY_Initial = GetBottomWorldY(initialLocalPos, initialLocalScale.y);
    }

    void Update()
    {
        if (hasShrunk) return;

        if (switch1 != null && switch1.IsPressed)
        {
            hasShrunk = true;
            PlayEffects();
            StartCoroutine(ShrinkUpwardsKeepingFloor());
        }
    }

    private void PlayEffects()
    {
        if (audioSource != null && dropSound != null)
            audioSource.PlayOneShot(dropSound);

        if (pointLight != null)
        {
            if (lightCoroutine != null) StopCoroutine(lightCoroutine);
            lightCoroutine = StartCoroutine(PulseLightEffect());
        }
    }

    private IEnumerator ShrinkUpwardsKeepingFloor()
    {
        float elapsed = 0f;
        float startScaleY = initialLocalScale.y;
        float endScaleY = 0f;

        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / shrinkDuration);

            // 1) 스케일 Y를 위쪽으로만 줄이기(= 실제로는 양쪽이지만…)
            float currentScaleY = Mathf.Lerp(startScaleY, endScaleY, t);

            // 2) 바닥 고정 보정: 현재 바닥 월드 Y를 계산하고, 최초 값과 맞추도록 로컬 위치를 조정
            Vector3 correctedLocalPos = SolveLocalPosForBottomLock(initialLocalPos, currentScaleY);

            transform.localScale = new Vector3(initialLocalScale.x, currentScaleY, initialLocalScale.z);
            transform.localPosition = correctedLocalPos;

            yield return null;
        }

        // 최종 고정
        transform.localScale = new Vector3(initialLocalScale.x, endScaleY, initialLocalScale.z);
        transform.localPosition = SolveLocalPosForBottomLock(initialLocalPos, endScaleY);
    }

    /// 바닥 월드 Y가 최초 값과 같도록 로컬 포지션을 보정
    private Vector3 SolveLocalPosForBottomLock(Vector3 baseLocalPos, float scaleY)
    {
        float currentBottomWorldY = GetBottomWorldY(baseLocalPos, scaleY);
        float deltaWorldY = bottomWorldY_Initial - currentBottomWorldY;

        // 부모 기준 로컬 델타로 변환해서 적용(부모 회전/스케일 있어도 안전)
        Transform parent = transform.parent;
        Vector3 localDelta = parent != null
            ? parent.InverseTransformVector(new Vector3(0f, deltaWorldY, 0f))
            : new Vector3(0f, deltaWorldY, 0f);

        return baseLocalPos + localDelta;
    }

    /// 주어진 로컬 포지션/스케일Y에서 바닥 포인트의 월드 Y를 계산
    private float GetBottomWorldY(Vector3 localPos, float scaleY)
    {
        Matrix4x4 parentL2W = transform.parent ? transform.parent.localToWorldMatrix : Matrix4x4.identity;

        // 초기 로컬 회전은 유지, 스케일만 변경
        Matrix4x4 localTRS = Matrix4x4.TRS(
            localPos,
            initialLocalRot,
            new Vector3(initialLocalScale.x, Mathf.Max(scaleY, 0f), initialLocalScale.z)
        );

        // 로컬 바닥 포인트(메쉬 바운즈 기준)
        Vector3 localBottomPoint = new Vector3(0f, bottomLocalY, 0f);

        // 월드로 변환
        Vector3 worldBottom = parentL2W.MultiplyPoint3x4(localTRS.MultiplyPoint3x4(localBottomPoint));
        return worldBottom.y;
    }

    private IEnumerator PulseLightEffect()
    {
        float half = lightDuration / 2f;
        float timer = 0f;

        pointLight.enabled = true;
        pointLight.range = 0f;
        pointLight.intensity = 0f;

        while (timer < half)
        {
            timer += Time.deltaTime;
            float t = timer / half;
            pointLight.range = Mathf.Lerp(0f, lightRange, t);
            pointLight.intensity = Mathf.Lerp(0f, lightIntensity, t);
            yield return null;
        }

        timer = 0f;
        while (timer < half)
        {
            timer += Time.deltaTime;
            float t = timer / half;
            pointLight.range = Mathf.Lerp(lightRange, 0f, t);
            pointLight.intensity = Mathf.Lerp(lightIntensity, 0f, t);
            yield return null;
        }

        pointLight.range = 0f;
        pointLight.intensity = 0f;
    }
}
