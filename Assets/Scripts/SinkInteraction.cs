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
    public float lightDuration = 2f;

    private bool isUsed = false;
    private GameObject screwdriverItem;

    public bool TryInteract(GameObject item)
    {
        if (isUsed || item.name != "ScrewDriver")
            return false;

        isUsed = true;
        screwdriverItem = item;
        screw.tag = "Untagged"; // 나사 줍기 방지

        FirstPersonController fpc = FindObjectOfType<FirstPersonController>();
        if (fpc != null)
        {
            fpc.CanMove = false;
        }

        StartCoroutine(UnscrewSequence(fpc));
        return false;
    }

    private IEnumerator UnscrewSequence(FirstPersonController fpc)
    {
        float unscrewDuration = 2f;
        float drainDuration = 2f;

        // 회전 준비
        Quaternion driverStartRot = screwdriverItem.transform.localRotation;
        Quaternion driverEndRot = driverStartRot * Quaternion.Euler(0f, 0f, 360f);
        Quaternion screwStartRot = screw.localRotation;
        Quaternion screwEndRot = screwStartRot * Quaternion.Euler(0f, 0f, 360f);

        if (audioSource && screwSoundClip)
        {
            audioSource.PlayOneShot(screwSoundClip);
            StartCoroutine(PlayLightEffect(lightDuration)); // 🔆 빛 효과 시작
        }

        float elapsed = 0f;
        while (elapsed < unscrewDuration)
        {
            float t = elapsed / unscrewDuration;
            screw.localRotation = Quaternion.Slerp(screwStartRot, screwEndRot, t);
            screwdriverItem.transform.localRotation = Quaternion.Slerp(driverStartRot, driverEndRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        screw.localRotation = screwEndRot;
        screwdriverItem.transform.localRotation = driverStartRot;

        if (audioSource && breakSoundClip)
        {
            audioSource.PlayOneShot(breakSoundClip);
            StartCoroutine(PlayLightEffect(breakSoundClip.length)); // 🔆 다시 빛 효과
        }

        yield return new WaitForSeconds(breakSoundClip.length);

        if (fpc != null)
        {
            fpc.ReleaseHeldObjectIfMatch(screwdriverItem);
            fpc.ResetHoldPosition();
            fpc.CanMove = true;
        }

        Destroy(screwdriverItem);

        // 물 빠짐
        elapsed = 0f;
        Vector3 wpStart = water.position;
        Vector3 wpEnd = new Vector3(wpStart.x, end.position.y, wpStart.z);
        Vector3 wsStart = water.localScale;
        Vector3 wsEnd = wsStart * 0.5f;

        if (audioSource && waterDrainClip)
        {
            audioSource.PlayOneShot(waterDrainClip);
        }

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
    }

    private IEnumerator PlayLightEffect(float duration)
    {
        if (pointLight == null)
            yield break;

        float elapsed = 0f;
        pointLight.enabled = true;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            pointLight.intensity = Mathf.Lerp(0f, lightIntensity, t);
            pointLight.range = Mathf.Lerp(0f, lightRange, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 부드럽게 사라짐
        elapsed = 0f;
        float fadeTime = 0.5f;
        float startIntensity = pointLight.intensity;
        float startRange = pointLight.range;

        while (elapsed < fadeTime)
        {
            float t = elapsed / fadeTime;
            pointLight.intensity = Mathf.Lerp(startIntensity, 0f, t);
            pointLight.range = Mathf.Lerp(startRange, 0f, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        pointLight.intensity = 0f;
        pointLight.range = 0f;
        pointLight.enabled = false;
    }
}
