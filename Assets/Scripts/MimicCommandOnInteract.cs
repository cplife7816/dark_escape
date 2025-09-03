// MimicCommandOnInteract.cs
using System.Collections.Generic;
using UnityEngine;

public class MimicCommandOnInteract : MonoBehaviour
{
    [Header("Elected Gate")]
    [SerializeField] private bool isElected = true;     // ← 플래그
    [SerializeField] private float overrideChaseSeconds = 3f; // 플레이어 상태 무시 추격 유지시간

    [Header("Walker Search")]
    [SerializeField] private float commandRadius = 25f;
    [SerializeField] private LayerMask walkerMask;

    [Header("Effects (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip commandClip;
    [SerializeField] private Light pointLight;
    [SerializeField] private float pulseDuration = 0.35f;
    [SerializeField] private float pulseIntensity = 6f;
    [SerializeField] private float pulseRange = 5f;

    // 외부에서 토글하고 싶을 수 있으니 프로퍼티도 제공
    public bool IsElected { get => isElected; set => isElected = value; }

    public void InteractByPlayer(FirstPersonController player)
    {
        // (1) 선거 여부 플래그 체크: false면 완전 무시
        if (!isElected || player == null) return;

        // (2) 효과(소리+라이트)
        PlayCommandEffects();

        // (3) 워커 수집
        List<WalkerAI> targets = FindWalkersAround();

        // (4) 강제 Rage + 즉시 추격(플레이어 상태 무시 오버라이드 포함)
        foreach (var w in targets)
        {
            if (w == null) continue;
            // 아래 WalkerAI에 추가한 오버라이드 지원 API 호출
            w.ForceEnterRageAndChase(player.transform, snapImmediate: true, overrideSeconds: overrideChaseSeconds);
        }
    }

    private List<WalkerAI> FindWalkersAround()
    {
        var result = new List<WalkerAI>();
        Collider[] hits = Physics.OverlapSphere(transform.position, commandRadius, walkerMask.value == 0 ? ~0 : walkerMask);
        foreach (var h in hits)
        {
            var w = h.GetComponentInParent<WalkerAI>();
            if (w != null && !result.Contains(w)) result.Add(w);
        }

        if (result.Count == 0)
        {
            var tagged = GameObject.FindGameObjectsWithTag("Whisper");
            foreach (var go in tagged)
            {
                if ((go.transform.position - transform.position).sqrMagnitude <= commandRadius * commandRadius)
                {
                    var w = go.GetComponent<WalkerAI>();
                    if (w != null && !result.Contains(w)) result.Add(w);
                }
            }
        }
        return result;
    }

    private void PlayCommandEffects()
    {
        if (audioSource && commandClip) audioSource.PlayOneShot(commandClip);
        if (pointLight && pulseDuration > 0f)
            StartCoroutine(PulseOnce(pointLight, pulseDuration, pulseRange, pulseIntensity));
    }

    private System.Collections.IEnumerator PulseOnce(Light p, float dur, float range, float intensity)
    {
        float half = dur * 0.5f;
        float t = 0f;
        float r0 = p.range;
        float i0 = p.intensity;
        while (t < half)
        {
            t += Time.deltaTime;
            float k = t / half;
            p.range = Mathf.Lerp(r0, range, k);
            p.intensity = Mathf.Lerp(i0, intensity, k);
            yield return null;
        }
        t = 0f;
        float r1 = p.range;
        float i1 = p.intensity;
        while (t < half)
        {
            t += Time.deltaTime;
            float k = t / half;
            p.range = Mathf.Lerp(r1, 0f, k);
            p.intensity = Mathf.Lerp(i1, 0f, k);
            yield return null;
        }
        p.range = 0f;
        p.intensity = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, commandRadius);
    }
}
