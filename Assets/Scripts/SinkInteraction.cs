using System.Collections;
using UnityEngine;

public class SinkInteraction : MonoBehaviour, IItemSocket
{
    [Header("Objects")]
    public Transform screw;
    public Transform water;
    public Transform start;
    public Transform end;

    [Header("Sounds")]
    public AudioSource audioSource;
    public AudioClip screwSoundClip;
    public AudioClip breakSoundClip;
    public AudioClip waterDrainClip;

    [Header("Lights")]
    public Light pointLight;
    public float lightIntensity = 3f;
    public float lightRange = 5f;
    public float lightFadeTime = 1f;

    private bool isUsed = false;
    private GameObject screwdriverItem;
    private Coroutine lightCoroutine;

    public bool TryInteract(GameObject item)
    {
        if (isUsed || item.name != "ScrewDriver")
            return false;

        isUsed = true;
        screwdriverItem = item;
        screw.tag = "Untagged";

        FirstPersonController fpc = FindObjectOfType<FirstPersonController>();
        if (fpc != null && breakSoundClip != null)
        {
            float totalDuration = 2f + breakSoundClip.length;
            fpc.PauseMovementFor(totalDuration);
        }

        StartCoroutine(UnscrewSequence());
        return false;
    }

    private IEnumerator UnscrewSequence()
    {
        float unscrewDuration = 2f;
        float drainDuration = 2f;

        FirstPersonController fpc = FindObjectOfType<FirstPersonController>();

        // ✅ 빛 점점 켜짐
        StartLightFadeIn();

        // 🔊 나사 회전 + 사운드
        Quaternion driverStartRot = screwdriverItem.transform.localRotation;
        Quaternion driverEndRot = driverStartRot * Quaternion.Euler(0f, 0f, 360f);
        Vector3 screwStartEuler = screw.localEulerAngles;
        Vector3 screwEndEuler = screwStartEuler + new Vector3(0f, 0f, 360f);

        if (audioSource && screwSoundClip)
            audioSource.PlayOneShot(screwSoundClip);

        float elapsed = 0f;
        while (elapsed < unscrewDuration)
        {
            float t = elapsed / unscrewDuration;
            screw.localEulerAngles = Vector3.Lerp(screwStartEuler, screwEndEuler, t);
            screwdriverItem.transform.localRotation = Quaternion.Slerp(driverStartRot, driverEndRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        screw.localEulerAngles = screwEndEuler;
        screwdriverItem.transform.localRotation = driverStartRot;

        screw.tag = "Untagged";
        if (screw.TryGetComponent<Collider>(out var col)) col.enabled = false;

        // 💥 파열음 + 드라이버 제거
        if (audioSource && breakSoundClip)
            audioSource.PlayOneShot(breakSoundClip);

        if (fpc != null)
        {
            fpc.ReleaseHeldObjectIfMatch(screwdriverItem);
            fpc.ResetHoldPosition();
        }
        Destroy(screwdriverItem);

        yield return new WaitForSeconds(breakSoundClip.length);
        yield return new WaitForSeconds(2f); // 관찰 시간

        // 💧 물 빠짐
        elapsed = 0f;
        Vector3 wpStart = water.position;
        Vector3 wpEnd = new Vector3(wpStart.x, end.position.y, wpStart.z);
        Vector3 wsStart = water.localScale;
        Vector3 wsEnd = wsStart * 0.5f;

        if (audioSource && waterDrainClip)
            audioSource.PlayOneShot(waterDrainClip);

        while (elapsed < drainDuration)
        {
            float t = elapsed / drainDuration;
            water.position = Vector3.Lerp(wpStart, wpEnd, t);
            water.localScale = Vector3.Lerp(wsStart, wsEnd, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        water.position = wpEnd;
        water.localScale = wsEnd;
        water.gameObject.SetActive(false);

        // ✅ 물 빠짐 끝나고 빛 서서히 꺼짐
        StartLightFadeOut();
    }

    private void StartLightFadeIn()
    {
        if (pointLight == null) return;
        if (lightCoroutine != null) StopCoroutine(lightCoroutine);
        lightCoroutine = StartCoroutine(FadeLight(0f, lightIntensity, 0f, lightRange, lightFadeTime));
    }

    private void StartLightFadeOut()
    {
        if (pointLight == null) return;
        if (lightCoroutine != null) StopCoroutine(lightCoroutine);
        lightCoroutine = StartCoroutine(FadeLight(lightIntensity, 0f, lightRange, 0f, lightFadeTime));
    }

    private IEnumerator FadeLight(float fromIntensity, float toIntensity, float fromRange, float toRange, float duration)
    {
        float elapsed = 0f;
        pointLight.enabled = true;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            pointLight.intensity = Mathf.Lerp(fromIntensity, toIntensity, t);
            pointLight.range = Mathf.Lerp(fromRange, toRange, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        pointLight.intensity = toIntensity;
        pointLight.range = toRange;

        if (toIntensity == 0f && toRange == 0f)
            pointLight.enabled = false;
    }
}
