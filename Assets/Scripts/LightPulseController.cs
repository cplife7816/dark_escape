using System.Collections;
using UnityEngine;

public class LightPulseController : MonoBehaviour
{
    public static LightPulseController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public void TriggerPulse(Light pointLight, float range, float intensity, float duration, MonoBehaviour caller)
    {
        if (pointLight == null || caller == null) return;

        // 기존 coroutine이 있을 경우 중단
        caller.StopAllCoroutines();
        caller.StartCoroutine(Pulse(pointLight, range, intensity, duration));
    }

    private IEnumerator Pulse(Light light, float range, float intensity, float duration)
    {
        float half = duration / 2f;
        float t = 0f;
        float initialRange = light.range;
        float initialIntensity = light.intensity;

        while (t < half)
        {
            t += Time.deltaTime;
            float ratio = t / half;
            light.range = Mathf.Lerp(initialRange, range, ratio);
            light.intensity = Mathf.Lerp(initialIntensity, intensity, ratio);
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float ratio = t / half;
            light.range = Mathf.Lerp(range, 0f, ratio);
            light.intensity = Mathf.Lerp(intensity, 0f, ratio);
            yield return null;
        }

        light.range = 0f;
        light.intensity = 0f;
    }
}
