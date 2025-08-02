using System.Collections;
using UnityEngine;

public class SwitchButtonController : MonoBehaviour, ITryInteractable
{
    [Header("Button Reference")]
    [SerializeField] private Transform switchButton;

    [Header("Animation Settings")]
    [SerializeField] private float pressAngle = 12f;
    [SerializeField] private float pressDuration = 0.3f;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pressSound;

    [Header("Light Settings")]
    [SerializeField] private Light pointLight;
    [SerializeField] private float lightRange = 5f;
    [SerializeField] private float lightIntensity = 6f;
    [SerializeField] private float lightDuration = 0.5f;

    private bool isPressed = false; // 이미 눌렸는지 여부 추적
    public bool IsPressed => isPressed; // 외부에서 읽기 가능
    private Coroutine lightCoroutine;


    public void TryInteract()
    {
        if (isPressed) return;
        isPressed = true;

        StartCoroutine(PressButton());

        if (audioSource && pressSound)
            audioSource.PlayOneShot(pressSound);

        if (pointLight)
            TriggerLightPulse();

        gameObject.tag = "Untagged"; // 상호작용 비활성화
    }

    private IEnumerator PressButton()
    {
        Quaternion startRot = switchButton.localRotation;
        Quaternion endRot = startRot * Quaternion.Euler(pressAngle, 0f, 0f);

        float t = 0f;
        while (t < pressDuration)
        {
            switchButton.localRotation = Quaternion.Slerp(startRot, endRot, t / pressDuration);
            t += Time.deltaTime;
            yield return null;
        }

        switchButton.localRotation = endRot;
    }

    private void TriggerLightPulse()
    {
        if (lightCoroutine != null)
            StopCoroutine(lightCoroutine);

        lightCoroutine = StartCoroutine(PulseLight());
    }

    private IEnumerator PulseLight()
    {
        float half = lightDuration / 2f;
        float timer = 0f;

        while (timer < half)
        {
            timer += Time.deltaTime;
            float t = timer / half;
            pointLight.range = Mathf.Lerp(0f, lightRange, t);
            pointLight.intensity = Mathf.Lerp(0f, lightIntensity, t);
            yield return null;
        }

        timer = 0f;
        while (timer < half)
        {
            timer += Time.deltaTime;
            float t = timer / half;
            pointLight.range = Mathf.Lerp(lightRange, 0f, t);
            pointLight.intensity = Mathf.Lerp(lightIntensity, 0f, t);
            yield return null;
        }

        pointLight.range = 0f;
        pointLight.intensity = 0f;
    }
}
