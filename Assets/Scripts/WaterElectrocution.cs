using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class WaterElectrocution : MonoBehaviour
{
    [Header("Detection (���� ����)")]
    [Tooltip("�浹ü�� �� �±׸� ���� ��� �÷��̾�� �����մϴ�. ����θ� FirstPersonController ���� ���ηε� �Ǻ��մϴ�.")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("���� ���� ���� �������� �����ϴ�. false�� ������ ���� ������ �ٽ� Ʈ���ŵ� �� �ֽ��ϴ�.")]
    [SerializeField] private bool blockReentryUntilReset = true;

    [Tooltip("�ܺ� ����ġ/���� ������ ���� ���� �������� ���θ� �����մϴ�.")]
    [SerializeField] private bool isElected = false;

    [Header("Electrocution SFX (���� ȿ����)")]
    [SerializeField] private AudioSource sfxSource;      // ����θ� ���� ������Ʈ�� AudioSource�� �ڵ� Ž��
    [SerializeField] private AudioClip electrocuteClip;  // ��½- ȿ����(ī�޶� ��ǰ� ���� ���)
    [SerializeField, Range(0f, 1f)] private float electrocuteVolume = 1f;

    [Header("Walker-Style Tint (ȭ�� ��������)")]
    [SerializeField] private CanvasGroup overlayGroup;   // ��ü ȭ�� CanvasGroup
    [SerializeField] private Image overlayImage;         // Ǯ��ũ�� Image
    [SerializeField] private Color overlayColor = new Color(0.6f, 0.8f, 1f, 1f); // ���� ����(Ǫ����)
    [SerializeField] private float overlayFadeSeconds = 0.5f;

    [Header("Tint SFX (Walker ��İ� ���� ����)")]
    [SerializeField] private AudioSource tintAudioSource; // Tint ���� ����� �ҽ�(����: ����)
    [SerializeField] private AudioClip tintStartClip;     // Tint ���� ����
    [SerializeField] private AudioClip tintLoopClip;      // Tint ���� ����(����)
    [SerializeField, Range(0f, 1f)] private float tintVolume = 1f;
    [SerializeField] private bool fadeOutTintAfter = true;
    [SerializeField] private float tintFadeOutSeconds = 0.4f;

    [Header("Camera FX (ƿƮ + Y �ϰ�)")]
    [SerializeField] private float cameraTiltAngle = 90f;        // Z�� ��ǥ ȸ��
    [SerializeField] private float tiltDuration = 1.5f;          // ��ü �ҿ� �ð�
    [SerializeField] private AnimationCurve tiltCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("���� ���� ���� ī�޶��� localPosition.y�� 0���� ������ �����մϴ�.")]
    [SerializeField] private bool adjustCameraY = true;     // ī�޶� Y ���� ��� ����
    [SerializeField] private float targetCameraLocalY = -2f; // �÷��̾� �߽� ����, ��ǥ local Y (��: -2)

    // ���� ����
    private bool hasTriggered = false;

    /// <summary>�ܺ� ���� ����ġ ��� ȣ��</summary>
    public void SetElected(bool value) => isElected = value;

    private void Reset()
    {
        // ���� ������ Ʈ���� �ݶ��̴� ����
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;

        // ���� ������Ʈ�� AudioSource �ڵ� ����
        if (!sfxSource) sfxSource = GetComponent<AudioSource>();

        // �������� ��Ȱ�� ����
        if (overlayGroup) overlayGroup.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isElected) return;
        if (blockReentryUntilReset && hasTriggered) return;
        if (!IsPlayer(other)) return;

        hasTriggered = true;
        var fpc = other.GetComponentInParent<FirstPersonController>();
        StartCoroutine(ElectrocuteSequence(fpc));
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isElected) return;
        if (blockReentryUntilReset && hasTriggered) return;
        if (!IsPlayer(other)) return;

        hasTriggered = true;
        var fpc = other.GetComponentInParent<FirstPersonController>();
        StartCoroutine(ElectrocuteSequence(fpc));
    }

    private bool IsPlayer(Collider other)
    {
        if (!string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag)) return true;
        return other.GetComponentInParent<FirstPersonController>() != null;
    }

    private IEnumerator ElectrocuteSequence(FirstPersonController player)
    {
        // 0) �̵�/�Է� ����: CanMove�� �ܺ� set �Ұ� �� ������Ʈ ��Ȱ��ȭ�� ���� ����
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
        // 1) ī�޶� ���(ƿƮ + Y �ϰ�) + "���� ȿ����" ���ÿ�
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

                    // ȸ�� ����
                    cam.transform.localRotation = Quaternion.Slerp(startRot, targetRot, k);

                    // ��ġ ����(Y��0)
                    if (adjustCameraY)
                        cam.transform.localPosition = Vector3.Lerp(startLocalPos, targetLocalPos, k);

                    yield return null;
                }

                cam.transform.localRotation = targetRot;
                if (adjustCameraY) cam.transform.localPosition = targetLocalPos;
            }
        }

        // ------------------------------
        // 2) Tint(��������) + Tint SFX
        // ------------------------------
        if (overlayGroup)
        {
            overlayGroup.gameObject.SetActive(true);
            if (overlayImage) overlayImage.color = overlayColor;
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

        // (����) ���ӿ��� ���� �����̸� ��Ȱ�� ����,
        // ȸ�� ���� ����� �Ʒ� ���� ���:
        // if (player) { player.enabled = prevFpcEnabled; if (cc) cc.enabled = prevCcEnabled; }

        if (fadeOutTintAfter && tintAudioSource && tintAudioSource.isPlaying)
            yield return StartCoroutine(FadeOutAndStop(tintAudioSource, tintFadeOutSeconds));

        if (!blockReentryUntilReset) hasTriggered = false;
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
