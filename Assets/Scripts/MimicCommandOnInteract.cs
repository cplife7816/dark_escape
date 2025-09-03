// MimicCommandOnInteract.cs
using System.Collections.Generic;
using UnityEngine;

public class MimicCommandOnInteract : MonoBehaviour
{
    [Header("Elected Gate")]
    [SerializeField] private bool isElected = true;     // �� �÷���
    [SerializeField] private float overrideChaseSeconds = 3f; // �÷��̾� ���� ���� �߰� �����ð�

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

    // �ܺο��� ����ϰ� ���� �� ������ ������Ƽ�� ����
    public bool IsElected { get => isElected; set => isElected = value; }

    public void InteractByPlayer(FirstPersonController player)
    {
        // (1) ���� ���� �÷��� üũ: false�� ���� ����
        if (!isElected || player == null) return;

        // (2) ȿ��(�Ҹ�+����Ʈ)
        PlayCommandEffects();

        // (3) ��Ŀ ����
        List<WalkerAI> targets = FindWalkersAround();

        // (4) ���� Rage + ��� �߰�(�÷��̾� ���� ���� �������̵� ����)
        foreach (var w in targets)
        {
            if (w == null) continue;
            // �Ʒ� WalkerAI�� �߰��� �������̵� ���� API ȣ��
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
