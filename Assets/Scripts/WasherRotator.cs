using UnityEngine;
using System.Collections;

namespace PxP
{
    [RequireComponent(typeof(MeshFilter))]
    [DisallowMultipleComponent]
    public class WasherRotator : MonoBehaviour
    {
        [Header("Rotation")]
        [Tooltip("�ʴ� ȸ�� ���� (deg/sec). ������ �⺻ ������ �ݽð�")]
        [SerializeField] private float rotationSpeedDegPerSec = 180f;

        [Tooltip("�� ����Ŭ���� �� ������ ���� (>=1)")]
        [Min(1)]
        [SerializeField] private int rotationsPerCycle = 5;

        [Header("Pauses")]
        [Tooltip("�� ����(360��)�� ��ĥ ������ ���� �ð�(��)")]
        [SerializeField] private float shortPauseSeconds = 0.5f;

        [Tooltip("ȸ������ŭ �� ���� �� �� ���� �ð�(��)")]
        [SerializeField] private float longPauseSeconds = 2.0f;

        [Header("One-Shot Sounds (Optional)")]
        [Tooltip("�� ���� ���� �� ���� ����Ǵ� ȿ������")]
        [SerializeField] private AudioClip[] rotationClips;

        [Tooltip("���� ȿ������ ����� AudioSource (������ �ڵ� �߰�)")]
        [SerializeField] private AudioSource audioSource;

        [Header("Continuous Loop Sound (Optional)")]
        [Tooltip("���� �ݺ����� ����� ������ AudioSource (������ �ڵ� �߰�)")]
        [SerializeField] private AudioSource loopAudioSource;

        [Tooltip("���� �ݺ����� ����� �Ҹ� Ŭ��")]
        [SerializeField] private AudioClip loopClip;

        [Range(0f, 1f)]
        [SerializeField] private float loopVolume = 0.6f;

        [SerializeField] private bool playLoopOnEnable = true;

        [Header("Direction Inversion (Per Cycle)")]
        [Tooltip("����Ŭ ������ �� Ƚ����ŭ ȸ���� '����'���� ������ �ٲ�.\n-1�̸� ���� ����.\n��) rotationsPerCycle=5, ��=2 �� 2ȸ ���� ����, ���� 3ȸ �ݴ� ����")]
        [SerializeField] private int reverseAfterRotations = -1;

        private MeshFilter meshFilter;
        private MeshCollider meshCollider;
        private Coroutine loopRoutine;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshCollider = GetComponent<MeshCollider>();

            // ���� ����� ������ҽ�
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                }
            }

            // ���� ����� ������ҽ�
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

            // �ʱ� �⺻ ����(��ȣ). ����Ŭ ���� �� �׻� �� �������� ������.
            int baseSign = (rotationSpeedDegPerSec >= 0f) ? 1 : -1;

            while (true)
            {
                // ����Ŭ���� ���� ����/������ ���
                bool invertEnabledThisCycle = (reverseAfterRotations >= 0) && (reverseAfterRotations < perCycle);

                for (int i = 0; i < perCycle; i++)
                {
                    // i��° ȸ��(0-based). reverseAfterRotations ���ĺ��� �ݴ��.
                    bool useInverted = invertEnabledThisCycle && (i >= reverseAfterRotations);
                    int effectiveSign = useInverted ? -baseSign : baseSign;

                    // �� ����(360��) ȸ��
                    yield return RotateOnce360(effectiveSign * baseSpeedAbs);

                    if (shortWait > 0f) yield return new WaitForSeconds(shortWait);
                }

                // ����Ŭ ���� �� �ڵ����� ���� ����Ŭ���� baseSign ����(�ڵ�� baseSign�� ����)
                if (longWait > 0f) yield return new WaitForSeconds(longWait);
            }
        }

        /// <summary>
        /// ��Ȯ�� 360�� Z�� ȸ��(������ �帮��Ʈ ����) + ���� �� ���� ���� ���
        /// </summary>
        private IEnumerator RotateOnce360(float signedDegPerSec)
        {
            // ���� ����
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

            // -1 ���(���� ����). -1 �̸��̸� -1��, 0 �̻��̸� 0~perCycle�� Ŭ����.
            if (reverseAfterRotations < -1) reverseAfterRotations = -1;
            if (reverseAfterRotations >= 0 && reverseAfterRotations > rotationsPerCycle)
                reverseAfterRotations = rotationsPerCycle; // perCycle�� ������ "���� 0ȸ"�� ��ǻ� ���� ����
        }
#endif
    }
}
