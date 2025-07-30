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

        // 💡 빛 펄스
        if (pointLight != null && LightPulseController.Instance != null)
        {
            LightPulseController.Instance.TriggerPulse(pointLight, lightRange, lightIntensity, lightDuration, this);
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
}
