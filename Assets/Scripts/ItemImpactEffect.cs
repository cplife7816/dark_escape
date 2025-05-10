using UnityEngine;
using System.Collections;

public class ItemImpactEffect : MonoBehaviour
{
    [Header("Light Settings")]
    [SerializeField] private Light pointLight;
    [SerializeField] private float maxRange = 5f;
    [SerializeField] private float pulseDuration = 0.5f;

    [Header("Sound Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] impactSounds;

    private Coroutine pulseCoroutine;

    private void OnCollisionEnter(Collision collision)
    {
        if (pointLight != null)
        {
            if (pulseCoroutine != null)
                StopCoroutine(pulseCoroutine);

            pulseCoroutine = StartCoroutine(PulseLight());
        }

        if (audioSource != null && impactSounds != null && impactSounds.Length > 0)
        {
            int index = Random.Range(0, impactSounds.Length);
            audioSource.PlayOneShot(impactSounds[index]);
        }
    }

    private IEnumerator PulseLight()
    {
        float startRange = pointLight.range;
        float halfDuration = pulseDuration / 2f;
        float timer = 0f;

        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            pointLight.range = Mathf.Lerp(startRange, maxRange, timer / halfDuration);
            yield return null;
        }

        timer = 0f;
        startRange = pointLight.range;

        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            pointLight.range = Mathf.Lerp(startRange, 0f, timer / halfDuration);
            yield return null;
        }

        pointLight.range = 0f;
    }
}
