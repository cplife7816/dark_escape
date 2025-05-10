using System.Collections;
using UnityEngine;

public class CassettePlayerController : MonoBehaviour
{
    [Header("Insert Slot")]
    [SerializeField] private Transform insertPosition;

    [Header("Effect Sequence Objects")]
    [SerializeField] private Transform firstObjectToRotate;
    [SerializeField] private Transform secondObjectToMove;

    [Header("Sound Effects")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip insertClip;
    [SerializeField] private AudioClip rotateClip;
    [SerializeField] private AudioClip moveClip;

    [Header("Animation Settings")]
    [SerializeField] private float rotationAmount = 15f;
    [SerializeField] private float moveAmount = 0.05f;

    private bool hasCassetteInserted = false;
    private GameObject insertedCassette;

    [Header("Light Effect")]
    [SerializeField] private Light pointLight;
    [SerializeField] private float pointLightIntensity = 7f;
    [SerializeField] private float lightMaxRange = 7f;
    [SerializeField] private float lightChangeSpeed = 5f;

    private Coroutine lightCoroutine = null;

    private void PulseLight()
    {
        if (lightCoroutine != null)
            StopCoroutine(lightCoroutine);

        lightCoroutine = StartCoroutine(LerpLightRange(lightMaxRange, pointLightIntensity));
    }

    private IEnumerator MaintainLightWhilePlaying(AudioSource source)
    {
        // 즉시 빛 키우기
        if (lightCoroutine != null)
            StopCoroutine(lightCoroutine);
        pointLight.range = lightMaxRange;

        // mp3가 재생 중이면 계속 유지
        while (source.isPlaying)
        {
            yield return null;
        }

        // 다 끝난 후에만 천천히 줄이기 시작
        lightCoroutine = StartCoroutine(LerpLightRange(0f, pointLightIntensity));
    }

    public void StartCassetteSequence(GameObject cassette)
    {
        if (hasCassetteInserted) return;

        insertedCassette = cassette;
        hasCassetteInserted = true;
        StartCoroutine(HandleCassetteSequence());
    }

    private IEnumerator HandleCassetteSequence()
    {
        // 1단계: 삽입 효과음
        if (audioSource && insertClip)
        {
            PulseLight(); 
            audioSource.PlayOneShot(insertClip);
        }

        yield return new WaitForSeconds(1f);

        // 2단계: 첫 번째 오브젝트 회전 애니메이션
        if (firstObjectToRotate)
        {
            Quaternion startRot = firstObjectToRotate.localRotation;
            Quaternion targetRot = startRot * Quaternion.Euler(0f, 0f, rotationAmount);
            yield return StartCoroutine(RotateOverTime(firstObjectToRotate, startRot, targetRot, 1.3f));

            if (audioSource && rotateClip)
            {
                PulseLight(); // 💡 회전 효과
                audioSource.PlayOneShot(rotateClip);
            }
        }

        yield return new WaitForSeconds(0.4f);

        // 3단계: 두 번째 오브젝트 이동 애니메이션
        if (secondObjectToMove)
        {
            Vector3 startPos = secondObjectToMove.localPosition;
            Vector3 targetPos = startPos + new Vector3(0f, moveAmount, 0f);
            yield return StartCoroutine(MoveOverTime(secondObjectToMove, startPos, targetPos, 0.75f));

            if (audioSource && moveClip)
            {
                PulseLight(); // 💡 이동 효과
                audioSource.PlayOneShot(moveClip);
            }
        }
        yield return new WaitForSeconds(moveClip != null ? moveClip.length : 1f);

        if (insertedCassette != null)
        {
            string clipName = insertedCassette.name.ToLower(); // 예: "cassette1"
            AudioClip cassetteAudio = Resources.Load<AudioClip>($"Audio/Cassettes/{clipName}");

            if (cassetteAudio != null)
            {
                audioSource.clip = cassetteAudio;
                audioSource.Play();
                StartCoroutine(MaintainLightWhilePlaying(audioSource));
                Debug.Log($"Playing cassette voice: {clipName}");
            }
            else
            {
                Debug.LogWarning($"Cassette audio '{clipName}' not found in Resources/Audio/Cassettes/");
            }
        }
    }

    private IEnumerator RotateOverTime(Transform target, Quaternion from, Quaternion to, float duration)
    {
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            target.localRotation = Quaternion.Slerp(from, to, t / duration);
            yield return null;
        }
        target.localRotation = to;
    }

    private IEnumerator MoveOverTime(Transform target, Vector3 from, Vector3 to, float duration)
    {
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            target.localPosition = Vector3.Lerp(from, to, t / duration);
            yield return null;
        }
        target.localPosition = to;
    }

    private IEnumerator LerpLightRange(float targetRange, float targetIntensity)
    {
        float rangeStart = pointLight.range;
        float intensityStart = pointLight.intensity;

        // ➤ 증가
        while (Mathf.Abs(pointLight.range - targetRange) > 0.01f)
        {
            pointLight.range = Mathf.Lerp(pointLight.range, targetRange, Time.deltaTime * lightChangeSpeed);
            pointLight.intensity = Mathf.Lerp(pointLight.intensity, targetIntensity, Time.deltaTime * lightChangeSpeed);
            yield return null;
        }

        // 유지
        pointLight.range = targetRange;
        pointLight.intensity = targetIntensity;

        // ➤ 감소
        while (pointLight.range > 0.01f)
        {
            pointLight.range = Mathf.Lerp(pointLight.range, 0f, Time.deltaTime * (lightChangeSpeed / 3f));
            pointLight.intensity = Mathf.Lerp(pointLight.intensity, 0f, Time.deltaTime * (lightChangeSpeed / 3f));
            yield return null;
        }

        pointLight.range = 0f;
        pointLight.intensity = 0f;
    }

}
