using UnityEngine;
using System.Collections;

public class EnemyFootsteps : MonoBehaviour
{
    public AudioSource footstepAudioSource;

    [Header("Footstep Sounds")]
    public AudioClip[] grassFootsteps;
    public AudioClip[] rockFootsteps;
    public AudioClip[] woodFootsteps;

    [Header("Point Lights")]
    public Light monsterPointLight1;
    public Light monsterPointLight2;
    public float lightPulseDuration = 0.2f;
    public float lightDecayDuration = 1.5f;
    public float maxRange = 5f;

    private Coroutine lightPulseCoroutine;
    private Coroutine lightDecayCoroutine;

    private void Start()
    {
        if (footstepAudioSource == null)
        {
            footstepAudioSource = GetComponent<AudioSource>();
        }
    }

    public void PlayFootstep()
    {
        AudioClip clip = GetRandomFootstepClip();
        if (clip != null && footstepAudioSource != null)
        {
            footstepAudioSource.PlayOneShot(clip);
        }

        if (monsterPointLight1 != null && monsterPointLight2 != null)
        {
            // 라이트 증폭 Coroutine 실행
            if (lightPulseCoroutine != null) StopCoroutine(lightPulseCoroutine);
            lightPulseCoroutine = StartCoroutine(PulseLight());

            // 기존 감쇠 Coroutine은 중단 후 다시 실행
            if (lightDecayCoroutine != null) StopCoroutine(lightDecayCoroutine);
            lightDecayCoroutine = StartCoroutine(DecayLight());
        }
    }

    private AudioClip GetRandomFootstepClip()
    {
        AudioClip[] selectedClips = rockFootsteps;

        if (selectedClips.Length > 0)
        {
            return selectedClips[Random.Range(0, selectedClips.Length)];
        }

        return null;
    }

    private IEnumerator PulseLight()
    {
        float start1 = monsterPointLight1.range;
        float start2 = monsterPointLight2.range;
        float elapsed = 0f;

        while (elapsed < lightPulseDuration)
        {
            float t = elapsed / lightPulseDuration;
            monsterPointLight1.range = Mathf.Lerp(start1, maxRange, t);
            monsterPointLight2.range = Mathf.Lerp(start2, maxRange, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        monsterPointLight1.range = maxRange;
        monsterPointLight2.range = maxRange;
    }

    private IEnumerator DecayLight()
    {
        yield return new WaitForSeconds(0.1f); // 짧은 유지 시간

        float start1 = monsterPointLight1.range;
        float start2 = monsterPointLight2.range;
        float elapsed = 0f;

        while (elapsed < lightDecayDuration)
        {
            float t = elapsed / lightDecayDuration;
            monsterPointLight1.range = Mathf.Lerp(start1, 0, t);
            monsterPointLight2.range = Mathf.Lerp(start2, 0, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        monsterPointLight1.range = 0;
        monsterPointLight2.range = 0;
    }
}
