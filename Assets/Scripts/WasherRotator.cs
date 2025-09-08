using UnityEngine;
using System.Collections;

namespace PxP
{
    [RequireComponent(typeof(MeshFilter))]
    [DisallowMultipleComponent]
    public class WasherRotator : MonoBehaviour, IElectedReceiver // ✅ isElected 신호 수신
    {
        [Header("Rotation")]
        [Tooltip("초당 회전 각도 (deg/sec). 음수면 기본 방향이 반시계")]
        [SerializeField] private float rotationSpeedDegPerSec = 180f;

        [Tooltip("한 사이클에서 몇 바퀴를 돌지 (>=1)")]
        [Min(1)]
        [SerializeField] private int rotationsPerCycle = 5;

        [Header("Pauses")]
        [Tooltip("한 바퀴(360°)를 마칠 때마다 쉬는 시간(초)")]
        [SerializeField] private float shortPauseSeconds = 0.5f;

        [Tooltip("회전수만큼 다 돌고 난 뒤 쉬는 시간(초)")]
        [SerializeField] private float longPauseSeconds = 2.0f;

        [Header("One-Shot Sounds (Optional)")]
        [Tooltip("한 바퀴 시작 시 랜덤 재생되는 효과음들")]
        [SerializeField] private AudioClip[] rotationClips;

        [Tooltip("원샷 효과음을 재생할 AudioSource (없으면 자동 추가)")]
        [SerializeField] private AudioSource audioSource;

        [Header("Continuous Loop Sound (Optional)")]
        [Tooltip("무한 반복으로 재생할 루프용 AudioSource (없으면 자동 추가)")]
        [SerializeField] private AudioSource loopAudioSource;

        [Tooltip("무한 반복으로 재생할 소리 클립")]
        [SerializeField] private AudioClip loopClip;

        [Range(0f, 1f)]
        [SerializeField] private float loopVolume = 0.6f;

        [SerializeField] private bool playLoopOnEnable = true;

        [Header("Direction Inversion (Per Cycle)")]
        [Tooltip("사이클 내에서 이 횟수만큼 회전한 '이후'부터 방향을 바꿈.\n-1이면 역전 없음.\n예) rotationsPerCycle=5, 값=2 → 2회 원래 방향, 이후 3회 반대 방향")]
        [SerializeField] private int reverseAfterRotations = -1;

        // ─────────────────────────────────────────────────────────────
        [Header("Power / isElected")]
        [Tooltip("초기 전원 상태 (런타임엔 SetElected()로 제어)")]
        [SerializeField] private bool isElected = false;

        [Header("Run Light (while isElected=true)")]
        [SerializeField] private Light runLight;                 // ✔ 네가 지정
        [SerializeField] private float runLightRange = 6f;       // ✔ 네가 지정
        [SerializeField] private float runLightIntensity = 2.5f; // ✔ 네가 지정
        [SerializeField, Min(0.1f)] private float lightChangeSpeed = 5f;

        private Coroutine lightCo;
        // ─────────────────────────────────────────────────────────────

        private MeshFilter meshFilter;
        private MeshCollider meshCollider;
        private Coroutine loopRoutine;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshCollider = GetComponent<MeshCollider>();

            // 원샷 재생용 오디오소스
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                }
            }

            // 루프 재생용 오디오소스
            if (loopAudioSource == null)
                loopAudioSource = gameObject.AddComponent<AudioSource>();

            loopAudioSource.playOnAwake = false;
            loopAudioSource.loop = true;
            loopAudioSource.volume = loopVolume;
            loopAudioSource.clip = loopClip;

            // 라이트 초기값 안전하게 0
            if (runLight)
            {
                runLight.range = isElected ? runLightRange : 0f;
                runLight.intensity = isElected ? runLightIntensity : 0f;
            }
        }

        private void OnEnable()
        {
            // 전원이 켜져있을 때만 루프 사운드 자동 재생
            if (isElected && playLoopOnEnable && loopClip != null && loopAudioSource != null && !loopAudioSource.isPlaying)
                loopAudioSource.Play();

            if (loopRoutine == null)
                loopRoutine = StartCoroutine(WasherLoop());
        }

        private void OnDisable()
        {
            if (loopRoutine != null)
            {
                StopCoroutine(loopRoutine);
                loopRoutine = null;
            }

            if (loopAudioSource != null && loopAudioSource.isPlaying)
                loopAudioSource.Stop();

            if (lightCo != null)
            {
                StopCoroutine(lightCo);
                lightCo = null;
            }
        }

        // === IElectedReceiver: 스위치/전원으로부터 신호 수신 ===
        public void SetElected(bool value)
        {
            if (isElected == value) return;
            isElected = value;

            // 루프 사운드 on/off
            if (loopAudioSource != null)
            {
                if (isElected)
                {
                    if (loopClip != null && !loopAudioSource.isPlaying)
                        loopAudioSource.Play();
                }
                else
                {
                    if (loopAudioSource.isPlaying)
                        loopAudioSource.Stop();
                }
            }

            // 러닝 라이트 on/off (부드러운 전환)
            if (runLight)
            {
                if (lightCo != null) StopCoroutine(lightCo);
                lightCo = StartCoroutine(CoDriveRunLight(isElected));
            }
        }

        private IEnumerator WasherLoop()
        {
            float shortWait = Mathf.Max(0f, shortPauseSeconds);
            float longWait = Mathf.Max(0f, longPauseSeconds);
            int perCycle = Mathf.Max(1, rotationsPerCycle);

            float baseSpeedAbs = Mathf.Abs(rotationSpeedDegPerSec);
            if (Mathf.Approximately(baseSpeedAbs, 0f))
                yield break;

            // 초기 기본 방향(부호). 사이클 종료 시 항상 이 방향으로 복구됨.
            int baseSign = (rotationSpeedDegPerSec >= 0f) ? 1 : -1;

            while (true)
            {
                // 전원 꺼져있으면 대기
                if (!isElected)
                {
                    yield return null;
                    continue;
                }

                // 사이클별로 역전 여부/시점을 계산
                bool invertEnabledThisCycle = (reverseAfterRotations >= 0) && (reverseAfterRotations < perCycle);

                for (int i = 0; i < perCycle; i++)
                {
                    if (!isElected) break; // 중간에 전원 꺼지면 즉시 중단

                    // i번째 회전(0-based). reverseAfterRotations 이후부터 반대로.
                    bool useInverted = invertEnabledThisCycle && (i >= reverseAfterRotations);
                    int effectiveSign = useInverted ? -baseSign : baseSign;

                    // 한 바퀴(360°) 회전 (중간에 꺼지면 즉시 탈출)
                    yield return RotateOnce360(effectiveSign * baseSpeedAbs);
                    if (!isElected) break;

                    if (shortWait > 0f)
                    {
                        float t = 0f;
                        while (t < shortWait)
                        {
                            if (!isElected) break;
                            t += Time.deltaTime;
                            yield return null;
                        }
                        if (!isElected) break;
                    }
                }

                // 사이클 종료 대기
                if (isElected && longWait > 0f)
                {
                    float t = 0f;
                    while (t < longWait)
                    {
                        if (!isElected) break;
                        t += Time.deltaTime;
                        yield return null;
                    }
                }
            }
        }

        /// <summary>
        /// 정확히 360° Z축 회전(프레임 드리프트 방지) + 시작 시 랜덤 원샷 재생
        /// isElected=false로 바뀌면 즉시 중단
        /// </summary>
        private IEnumerator RotateOnce360(float signedDegPerSec)
        {
            if (!isElected) yield break;

            // 랜덤 원샷 (전원 켜져 있을 때만)
            if (rotationClips != null && rotationClips.Length > 0 && audioSource != null)
            {
                int idx = Random.Range(0, rotationClips.Length);
                var clip = rotationClips[idx];
                if (clip != null) audioSource.PlayOneShot(clip);
            }

            float speedAbs = Mathf.Abs(signedDegPerSec);
            if (Mathf.Approximately(speedAbs, 0f)) yield break;

            int sign = (signedDegPerSec >= 0f) ? 1 : -1;
            float remaining = 360f;

            while (remaining > 0f)
            {
                if (!isElected) yield break; // 🔌 전원 끊기면 즉시 중단

                float step = speedAbs * Time.deltaTime;
                if (step > remaining) step = remaining;

                transform.Rotate(0f, 0f, sign * step, Space.Self);
                remaining -= step;

                if (meshCollider != null && meshFilter != null && meshFilter.sharedMesh != null)
                {
                    meshCollider.sharedMesh = null;
                    meshCollider.sharedMesh = meshFilter.sharedMesh;
                }
                yield return null;
            }
        }

        // ── Light smooth drive ──────────────────────────────────────
        private IEnumerator CoDriveRunLight(bool turnOn)
        {
            float goalRange = turnOn ? runLightRange : 0f;
            float goalInt = turnOn ? runLightIntensity : 0f;

            while (runLight &&
                   (Mathf.Abs(runLight.range - goalRange) > 0.02f ||
                    Mathf.Abs(runLight.intensity - goalInt) > 0.02f))
            {
                runLight.range = Mathf.MoveTowards(runLight.range, goalRange, lightChangeSpeed * Time.deltaTime);
                runLight.intensity = Mathf.MoveTowards(runLight.intensity, goalInt, lightChangeSpeed * Time.deltaTime);
                yield return null;
            }

            if (runLight)
            {
                runLight.range = goalRange;
                runLight.intensity = goalInt;
            }

            lightCo = null;
        }
        // ────────────────────────────────────────────────────────────

        #region Public Setters
        public void SetRotationSpeedDegPerSec(float v) => rotationSpeedDegPerSec = v;
        public void SetRotationsPerCycle(int v) => rotationsPerCycle = Mathf.Max(1, v);
        public void SetShortPause(float v) => shortPauseSeconds = Mathf.Max(0f, v);
        public void SetLongPause(float v) => longPauseSeconds = Mathf.Max(0f, v);
        public void SetRotationClips(AudioClip[] a) => rotationClips = a;
        public void SetAudioSource(AudioSource s) => audioSource = s;
        public void SetLoopSource(AudioSource s)
        {
            loopAudioSource = s;
            if (loopAudioSource != null)
            {
                loopAudioSource.loop = true;
                loopAudioSource.playOnAwake = false;
                loopAudioSource.volume = loopVolume;
                loopAudioSource.clip = loopClip;
            }
        }
        public void SetLoopClip(AudioClip c)
        {
            loopClip = c;
            if (loopAudioSource != null) loopAudioSource.clip = loopClip;
        }
        public void SetLoopVolume(float v)
        {
            loopVolume = Mathf.Clamp01(v);
            if (loopAudioSource != null) loopAudioSource.volume = loopVolume;
        }
        public void SetPlayLoopOnEnable(bool on) => playLoopOnEnable = on;

        // 외부에서 전원 제어하고 싶을 때(스위치 말고도 사용 가능)
        public void SetPower(bool on) => SetElected(on);
        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (rotationsPerCycle < 1) rotationsPerCycle = 1;

            // -1 허용(역전 없음). -1 미만이면 -1로, 0 이상이면 0~perCycle로 클램프.
            if (reverseAfterRotations < -1) reverseAfterRotations = -1;
            if (reverseAfterRotations >= 0 && reverseAfterRotations > rotationsPerCycle)
                reverseAfterRotations = rotationsPerCycle; // perCycle과 같으면 사실상 역전 없음
        }
#endif
    }
}
