using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class WaterElectrocution : MonoBehaviour
{
    [Header("Detection (감전 감지)")]
    [Tooltip("충돌체가 이 태그를 가진 경우 플레이어로 간주합니다. 비워두면 FirstPersonController 존재 여부로도 판별합니다.")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("감전 중인 동안 재입장을 막습니다. false면 시퀀스 종료 전에도 다시 트리거될 수 있습니다.")]
    [SerializeField] private bool blockReentryUntilReset = true;

    [Tooltip("외부 스위치/누수 등으로 물이 전도 상태인지 여부를 제어합니다.")]
    [SerializeField] private bool isElected = false;

    [Header("Electrocution SFX (순간 효과음)")]
    [SerializeField] private AudioSource sfxSource;      // 비워두면 같은 오브젝트의 AudioSource를 자동 탐색
    [SerializeField] private AudioClip electrocuteClip;  // 번쩍- 효과음(카메라 모션과 동시 재생)
    [SerializeField, Range(0f, 1f)] private float electrocuteVolume = 1f;

    [Header("Walker-Style Tint (화면 오버레이)")]
    [SerializeField] private CanvasGroup overlayGroup;   // 전체 화면 CanvasGroup
    [SerializeField] private Image overlayImage;         // 풀스크린 Image
    [SerializeField] private Color overlayColor = new Color(0.6f, 0.8f, 1f, 1f); // 전기 느낌(푸른빛)
    [SerializeField] private float overlayFadeSeconds = 0.5f;

    [Header("Tint SFX (Walker 방식과 동일 컨셉)")]
    [SerializeField] private AudioSource tintAudioSource; // Tint 전용 오디오 소스(권장: 별도)
    [SerializeField] private AudioClip tintStartClip;     // Tint 시작 원샷
    [SerializeField] private AudioClip tintLoopClip;      // Tint 유지 루프(선택)
    [SerializeField, Range(0f, 1f)] private float tintVolume = 1f;
    [SerializeField] private bool fadeOutTintAfter = true;
    [SerializeField] private float tintFadeOutSeconds = 0.4f;

    [Header("Camera FX (틸트 + Y 하강)")]
    [SerializeField] private float cameraTiltAngle = 90f;        // Z축 목표 회전
    [SerializeField] private float tiltDuration = 1.5f;          // 전체 소요 시간
    [SerializeField] private AnimationCurve tiltCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("감전 연출 동안 카메라의 localPosition.y를 0으로 서서히 보정합니다.")]
    [SerializeField] private bool adjustCameraY = true;     // 카메라 Y 보정 사용 여부
    [SerializeField] private float targetCameraLocalY = -2f; // 플레이어 중심 기준, 목표 local Y (예: -2)

    [Header("Camera / Head (감전 중 흔드는 대상)")]
    [SerializeField] private Transform headOrCamera;   // 감전 중 건드리는 카메라/헤드 Transform (기존에 쓰던 대상 연결)
    private Vector3 _camDefaultLocalPos;
    private Quaternion _camDefaultLocalRot;

    [Header("Trigger / Collider (옵션)")]
    [SerializeField] private Collider electrocutionTrigger; // 감전 판정 콜라이더(있으면 복구)
    private int _triggerDefaultLayer;
    private bool _triggerDefaultEnabled;



    // 내부 상태
    private bool hasTriggered = false;

    /// <summary>외부 전기 스위치 등에서 호출</summary>
    public void SetElected(bool value) => isElected = value;

    private void Awake()
    {
        if (headOrCamera)
        {
            _camDefaultLocalPos = headOrCamera.localPosition;
            _camDefaultLocalRot = headOrCamera.localRotation;
        }
        if (electrocutionTrigger)
        {
            _triggerDefaultLayer = electrocutionTrigger.gameObject.layer;
            _triggerDefaultEnabled = electrocutionTrigger.enabled;
        }
    }

    // ------------------------
    // 1) SaveSystem.AfterLoad 구독/해제
    // ------------------------
    private void OnEnable()
    {
        Debug.Log("[ELEC] OnEnable");
        SaveSystem.AfterLoad += OnAfterLoad;
    }
    private void OnDisable()
    {
        Debug.Log("[ELEC] OnDisable");
        SaveSystem.AfterLoad -= OnAfterLoad;
    }

    private void OnAfterLoad()
    {
        Debug.Log("[ELEC] AfterLoad → ResetAfterLoad()");
        ResetAfterLoad();
    }

    public void ResetAfterLoad()
    {
        Debug.Log("[ELEC] ResetAfterLoad: stop coroutines + reset flags/UI/audio/camera/trigger");
        StopAllCoroutines();
        hasTriggered = false;

        if (overlayGroup)
        {
            overlayGroup.alpha = 0f;
            overlayGroup.interactable = false;
            overlayGroup.blocksRaycasts = false;
            overlayGroup.gameObject.SetActive(false);
        }
        if (overlayImage) overlayImage.enabled = false;

        if (tintAudioSource) tintAudioSource.Stop();
        if (sfxSource) sfxSource.Stop();

        if (headOrCamera)
        {
            headOrCamera.localPosition = _camDefaultLocalPos;
            headOrCamera.localRotation = _camDefaultLocalRot;
        }

        var fpc = FindObjectOfType<FirstPersonController>();
        if (fpc)
        {
            Debug.Log($"[ELEC] ResetAfterLoad → enable FPC (was {fpc.enabled})");
            fpc.enabled = true;
            var cc = fpc.GetComponent<CharacterController>();
            if (cc) { Debug.Log($"[ELEC] ResetAfterLoad → enable CC (was {cc.enabled})"); cc.enabled = true; }
        }

        if (electrocutionTrigger)
        {
            electrocutionTrigger.enabled = _triggerDefaultEnabled;
            electrocutionTrigger.gameObject.layer = _triggerDefaultLayer;
            Debug.Log($"[ELEC] ResetAfterLoad → trigger enabled={electrocutionTrigger.enabled}, layer={electrocutionTrigger.gameObject.layer}");
        }

        Debug.Log($"[ELEC] ResetAfterLoad DONE. isElected={isElected}, hasTriggered={hasTriggered}");
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[ELEC1] OnTriggerEnter: isElected={isElected}, hasTriggered={hasTriggered}, blockReentry={blockReentryUntilReset}");
        if (!isElected) return;
        if (blockReentryUntilReset && hasTriggered) return;
        if (!IsPlayer(other)) return;

        hasTriggered = true;
        Debug.Log("[ELEC] → Start ElectrocuteSequence (Enter)");
        var fpc = other.GetComponentInParent<FirstPersonController>();
        StartCoroutine(ElectrocuteSequence(fpc));
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isElected) return;
        if (blockReentryUntilReset && hasTriggered) return;
        if (!IsPlayer(other)) return;

        hasTriggered = true;
        Debug.Log("[ELEC] → Start ElectrocuteSequence (Stay)");
        var fpc = other.GetComponentInParent<FirstPersonController>();
        StartCoroutine(ElectrocuteSequence(fpc));
    }

    private void Reset()
    {
        // 감전 영역은 트리거 콜라이더 권장
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;

        // 같은 오브젝트의 AudioSource 자동 연결
        if (!sfxSource) sfxSource = GetComponent<AudioSource>();

        // 오버레이 비활성 시작
        if (overlayGroup) overlayGroup.gameObject.SetActive(false);
    }


    private bool IsPlayer(Collider other)
    {
        if (!string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag)) return true;
        return other.GetComponentInParent<FirstPersonController>() != null;
    }

    private IEnumerator ElectrocuteSequence(FirstPersonController player)
    {
        Debug.Log("[ELEC1] ElectrocuteSequence START");
        // 0) 이동/입력 정지: CanMove는 외부 set 불가 → 컴포넌트 비활성화로 안전 차단
        CharacterController cc = null;
        bool prevFpcEnabled = false, prevCcEnabled = false;

        if (player)
        {
            cc = player.GetComponent<CharacterController>();
            prevFpcEnabled = player.enabled;
            if (cc) { prevCcEnabled = cc.enabled; cc.enabled = false; }
            player.enabled = false;
        }

        // ------------------------------
        // 1) 카메라 모션(틸트 + Y 하강) + "순간 효과음" 동시에
        // ------------------------------
        var src = sfxSource ? sfxSource : GetComponent<AudioSource>();
        if (src && electrocuteClip)
            src.PlayOneShot(electrocuteClip, Mathf.Clamp01(electrocuteVolume));

        if (player)
        {
            var cam = player.GetComponentInChildren<Camera>();
            if (cam)
            {
                var startRot = cam.transform.localRotation;
                var targetRot = startRot * Quaternion.Euler(0f, 0f, cameraTiltAngle);

                var startLocalPos = cam.transform.localPosition;
                var targetLocalPos = startLocalPos;
                if (adjustCameraY)
                    targetLocalPos = new Vector3(startLocalPos.x, targetCameraLocalY, startLocalPos.z);

                float t = 0f;
                float dur = Mathf.Max(0.001f, tiltDuration);

                while (t < 1f)
                {
                    t += Time.deltaTime / dur;
                    float k = tiltCurve != null ? tiltCurve.Evaluate(t) : t;

                    // 회전 보간
                    cam.transform.localRotation = Quaternion.Slerp(startRot, targetRot, k);

                    // 위치 보간(Y→0)
                    if (adjustCameraY)
                        cam.transform.localPosition = Vector3.Lerp(startLocalPos, targetLocalPos, k);

                    yield return null;
                }

                cam.transform.localRotation = targetRot;
                if (adjustCameraY) cam.transform.localPosition = targetLocalPos;
            }
        }

        // ------------------------------
        // 2) Tint(오버레이) + Tint SFX
        // ------------------------------
        // WaterElectrocution.cs (ElectrocuteSequence 내부)
        if (overlayGroup)
        {
            overlayGroup.gameObject.SetActive(true);
            if (overlayImage) { overlayImage.enabled = true; overlayImage.color = overlayColor; }
            Debug.Log($"[ELEC1] Tint ON (enabled={overlayImage?.enabled})");
            yield return StartCoroutine(FadeOverlay(overlayGroup, 1f, overlayFadeSeconds));
        }


        if (tintAudioSource)
        {
            if (tintStartClip) tintAudioSource.PlayOneShot(tintStartClip, Mathf.Clamp01(tintVolume));

            if (tintLoopClip)
            {
                tintAudioSource.clip = tintLoopClip;
                tintAudioSource.loop = true;
                tintAudioSource.volume = tintVolume;
                tintAudioSource.Play();
            }
        }

        // (선택) 게임오버 고정 연출이면 비활성 유지,
        // 회생 가능 설계면 아래 복구 사용:
        // if (player) { player.enabled = prevFpcEnabled; if (cc) cc.enabled = prevCcEnabled; }

        if (fadeOutTintAfter && tintAudioSource && tintAudioSource.isPlaying)
            yield return StartCoroutine(FadeOutAndStop(tintAudioSource, tintFadeOutSeconds));

        if (!blockReentryUntilReset) hasTriggered = false;

        if (player != null)
        {
            Debug.Log("[ELEC1] ElectrocuteSequence END → ReturnToLastViaSaveSystem(0.5)");
            StartCoroutine(ReturnToLastViaSaveSystem(0.5f));
        }
        else
        {
            Debug.LogWarning("[ELEC] ElectrocuteSequence END but player=null");
        }
    }

    private IEnumerator ReturnToLastViaSaveSystem(float delay)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, delay));

        // 체크포인트 유무 로그(선택)
        bool has = SaveSystem.CheckpointStore.Has("_Last"); // 메모리 슬롯 확인
        Debug.Log($"[ELEC1] ReturnToLastViaSaveSystem → hasLast={has}");

        // 있으면 로드, 없으면(옵션) 현재 씬 리로드 등 처리 가능
        if (has && SaveSystem.Instance != null)
        {
            SaveSystem.Instance.LoadCheckpoint("_Last");
        }
        else
        {
            Debug.LogWarning("[ELEC] _Last slot missing or SaveSystem.Instance null.");
            // 필요하면 SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private IEnumerator FadeOverlay(CanvasGroup group, float target, float seconds)
    {
        float dur = Mathf.Max(0.001f, seconds);
        float start = group.alpha;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            group.alpha = Mathf.Lerp(start, target, t);
            yield return null;
        }
        group.alpha = target;
    }

    private IEnumerator FadeOutAndStop(AudioSource src, float seconds)
    {
        float dur = Mathf.Max(0.001f, seconds);
        float start = src.volume;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            src.volume = Mathf.Lerp(start, 0f, t);
            yield return null;
        }
        if (src.isPlaying) src.Stop();
        src.volume = start;
        src.loop = false;
        src.clip = null;
    }


}
