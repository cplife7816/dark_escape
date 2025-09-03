using UnityEngine;
using System.Collections;

namespace PxP
{
    [RequireComponent(typeof(MeshFilter))]
    [DisallowMultipleComponent]
    public class WasherRotator : MonoBehaviour
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
        }

        private void OnEnable()
        {
            if (playLoopOnEnable && loopClip != null && loopAudioSource != null && !loopAudioSource.isPlaying)
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
                // 사이클별로 역전 여부/시점을 계산
                bool invertEnabledThisCycle = (reverseAfterRotations >= 0) && (reverseAfterRotations < perCycle);

                for (int i = 0; i < perCycle; i++)
                {
                    // i번째 회전(0-based). reverseAfterRotations 이후부터 반대로.
                    bool useInverted = invertEnabledThisCycle && (i >= reverseAfterRotations);
                    int effectiveSign = useInverted ? -baseSign : baseSign;

                    // 한 바퀴(360°) 회전
                    yield return RotateOnce360(effectiveSign * baseSpeedAbs);

                    if (shortWait > 0f) yield return new WaitForSeconds(shortWait);
                }

                // 사이클 종료 → 자동으로 다음 사이클에서 baseSign 복구(코드상 baseSign을 유지)
                if (longWait > 0f) yield return new WaitForSeconds(longWait);
            }
        }

        /// <summary>
        /// 정확히 360° Z축 회전(프레임 드리프트 방지) + 시작 시 랜덤 원샷 재생
        /// </summary>
        private IEnumerator RotateOnce360(float signedDegPerSec)
        {
            // 랜덤 원샷
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
        public void SetReverseAfterRotations(int v) => reverseAfterRotations = v;
        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (rotationsPerCycle < 1) rotationsPerCycle = 1;

            // -1 허용(역전 없음). -1 미만이면 -1로, 0 이상이면 0~perCycle로 클램프.
            if (reverseAfterRotations < -1) reverseAfterRotations = -1;
            if (reverseAfterRotations >= 0 && reverseAfterRotations > rotationsPerCycle)
                reverseAfterRotations = rotationsPerCycle; // perCycle과 같으면 "남은 0회"라 사실상 역전 없음
        }
#endif
    }
}
