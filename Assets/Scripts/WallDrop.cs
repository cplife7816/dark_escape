using System.Collections;
using UnityEngine;

public class WallDrop : MonoBehaviour
{
    [Header("DNA Center Parts")]
    [SerializeField] private GameObject dnaCenter1;
    [SerializeField] private GameObject dnaCenter2;
    [SerializeField] private GameObject dnaCenter3;

    [Header("Target Transform (부모)")]
    [SerializeField] private Transform endPoint; // 벽의 부모 오브젝트 (로컬 기준점)

    [Header("Drop Settings")]
    [SerializeField] private float dropDuration = 2f;

    [Header("Sound Effect")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip dropSound;

    [Header("Light Effect")]
    [SerializeField] private Light pointLight;
    [SerializeField] private float lightRange = 5f;
    [SerializeField] private float lightIntensity = 7f;
    [SerializeField] private float lightDuration = 0.5f;

    private bool hasDropped = false;
    private Coroutine lightCoroutine;

    private void Update()
    {
        if (hasDropped) return;

        if (dnaCenter1.activeSelf && dnaCenter2.activeSelf && dnaCenter3.activeSelf)
        {
            hasDropped = true;
            PlayEffects();
            StartCoroutine(DropWallToZeroY());
        }
    }

    private void PlayEffects()
    {
        // 🔊 사운드 재생
        if (audioSource != null && dropSound != null)
            audioSource.PlayOneShot(dropSound);

        // 💡 빛 펄스 직접 실행
        if (pointLight != null)
        {
            if (lightCoroutine != null)
                StopCoroutine(lightCoroutine);

            lightCoroutine = StartCoroutine(PulseLightEffect());
        }
    }

    private IEnumerator DropWallToZeroY()
    {
        Vector3 startLocalPos = transform.localPosition;
        Vector3 targetLocalPos = new Vector3(startLocalPos.x, 0f, startLocalPos.z);

        float elapsed = 0f;

        while (elapsed < dropDuration)
        {
            float t = elapsed / dropDuration;
            transform.localPosition = Vector3.Lerp(startLocalPos, targetLocalPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = targetLocalPos;
    }

    private IEnumerator PulseLightEffect()
    {
        float half = lightDuration / 2f;
        float timer = 0f;

        float startRange = 0f;
        float startIntensity = 0f;

        pointLight.range = 0f;
        pointLight.intensity = 0f;
        pointLight.enabled = true; // 항상 켜짐 보장

        // ➤ 빛 커지기
        while (timer < half)
        {
            timer += Time.deltaTime;
            float t = timer / half;
            pointLight.range = Mathf.Lerp(startRange, lightRange, t);
            pointLight.intensity = Mathf.Lerp(startIntensity, lightIntensity, t);
            yield return null;
        }

        // ➤ 빛 줄어들기
        timer = 0f;
        startRange = pointLight.range;
        startIntensity = pointLight.intensity;

        while (timer < half)
        {
            timer += Time.deltaTime;
            float t = timer / half;
            pointLight.range = Mathf.Lerp(startRange, 0f, t);
            pointLight.intensity = Mathf.Lerp(startIntensity, 0f, t);
            yield return null;
        }

        pointLight.range = 0f;
        pointLight.intensity = 0f;
    }
}
