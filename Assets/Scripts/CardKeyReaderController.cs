using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public class CardKeyReaderController : MonoBehaviour, IItemSocket
{
    [Header("Card Key Name (Must Match Item.name)")]
    [SerializeField] private string requiredCardName;

    [Header("Inserted Card Visual")]
    [SerializeField] private GameObject insertedCardVisual;
    [SerializeField] private float insertedCardTargetY = 0.0f;
    [SerializeField] private float insertDuration = 0.6f;
    [SerializeField] private AudioClip insertSfx;

    [Header("Reader Switches (Move In Order)")]
    [SerializeField] private List<Transform> readerSwitches = new();
    [SerializeField] private float switchTargetX = 0.2f;
    [SerializeField] private float switchMoveDuration = 0.35f;
    [SerializeField] private float perSwitchDelay = 0.05f;
    [SerializeField] private AudioClip switchMoveSfx;

    [Header("Elevator Signal (Event-based)")]
    [Tooltip("엘리베이터(또는 리스너)에게 상태만 브로드캐스트: true = On")]
    public UnityEvent<bool> OnElectedChanged;

    [Header("Point Light (Hold During Animation)")]
    [SerializeField] private Light pointLight;
    [SerializeField] private float holdIntensity = 2.0f;
    [SerializeField] private float holdRange = 8.0f;
    [SerializeField] private bool revertLightAfter = true;
    [SerializeField] private float lightFadeOutSeconds = 0.6f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    private bool _used;
    private float _origIntensity, _origRange;
    private bool _origLightEnabled;
    private Coroutine lightCo;

    private void Awake()
    {
        if (audioSource == null) TryGetComponent(out audioSource);
        if (insertedCardVisual != null) insertedCardVisual.SetActive(false);
        if (!CompareTag("Card")) gameObject.tag = "Card"; // 강제 태그

        if (pointLight != null)
        {
            _origLightEnabled = pointLight.enabled;
            _origIntensity = pointLight.intensity;
            _origRange = pointLight.range;
        }
    }

    public bool CanInteract(GameObject item)
    {
        if (_used || item == null || string.IsNullOrEmpty(requiredCardName)) return false;
        if (!CompareTag("Card")) return false;
        return item.name == requiredCardName;
    }

    public bool TryInteract(GameObject item)
    {
        if (!CanInteract(item)) return false;
        _used = true;

        item.SetActive(false); // 들고 있던 카드 숨김
        StartCoroutine(Co_InsertAndSwitchSequence());
        return true;
    }

    private IEnumerator Co_InsertAndSwitchSequence()
    {
        EnableHoldLight(true);

        if (insertedCardVisual != null)
        {
            insertedCardVisual.SetActive(true);
            Vector3 s = insertedCardVisual.transform.localPosition;
            Vector3 e = new Vector3(s.x, insertedCardTargetY, s.z);
            TryPlay(insertSfx);
            yield return LerpLocalPosition(insertedCardVisual.transform, s, e, insertDuration);
        }

        for (int i = 0; i < readerSwitches.Count; i++)
        {
            var sw = readerSwitches[i];
            if (sw == null) continue;

            Vector3 s = sw.localPosition;
            Vector3 e = new Vector3(switchTargetX, s.y, s.z);
            TryPlay(switchMoveSfx);
            yield return LerpLocalPosition(sw, s, e, switchMoveDuration);
            if (perSwitchDelay > 0f) yield return new WaitForSeconds(perSwitchDelay);
        }

        // ✅ 오직 이벤트로만 브로드캐스트
        OnElectedChanged?.Invoke(true);

        if (revertLightAfter) EnableHoldLight(false);
    }

    private IEnumerator LerpLocalPosition(Transform t, Vector3 s, Vector3 e, float d)
    {
        if (t == null) yield break;
        if (d <= 0f) { t.localPosition = e; yield break; }
        float time = 0f;
        while (time < d)
        {
            time += Time.deltaTime;
            t.localPosition = Vector3.Lerp(s, e, time / d);
            yield return null;
        }
        t.localPosition = e;
    }

    private void TryPlay(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }

    private void EnableHoldLight(bool on)
    {
        if (pointLight == null) return;

        if (on)
        {
            _origLightEnabled = pointLight.enabled;
            _origIntensity = pointLight.intensity;
            _origRange = pointLight.range;

            if (lightCo != null) StopCoroutine(lightCo);
            pointLight.enabled = true;
            pointLight.intensity = holdIntensity;
            pointLight.range = holdRange;
        }
        else
        {
            if (lightCo != null) StopCoroutine(lightCo);
            lightCo = StartCoroutine(Co_FadeLightTo(0f, 0f, lightFadeOutSeconds, restoreOriginalAfter: true));
        }
    }

    private IEnumerator Co_FadeLightTo(float targetRange, float targetIntensity, float seconds, bool restoreOriginalAfter)
    {
        if (pointLight == null) yield break;
        seconds = Mathf.Max(0.001f, seconds);

        float sr = pointLight.range;
        float si = pointLight.intensity;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / seconds;
            pointLight.range = Mathf.Lerp(sr, targetRange, t);
            pointLight.intensity = Mathf.Lerp(si, targetIntensity, t);
            yield return null;
        }
        pointLight.range = targetRange;
        pointLight.intensity = targetIntensity;

        if (restoreOriginalAfter)
        {
            pointLight.enabled = _origLightEnabled;
            pointLight.intensity = _origIntensity;
            pointLight.range = _origRange;
        }
    }
}
