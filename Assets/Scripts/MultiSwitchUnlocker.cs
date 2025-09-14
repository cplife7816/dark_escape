using UnityEngine;

public class MultiSwitchUnlocker : MonoBehaviour
{
    [Header("Target Windows (Both Sides)")]
    [SerializeField] private WindowInteraction[] windowsToUnlock;

    [Header("Switch References")]
    [SerializeField] private SwitchButtonController switch1;
    [SerializeField] private SwitchButtonController switch2;
    [SerializeField] private SwitchButtonController switch3;

    [Header("Unlock Feedback")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip unlockSound;

    [SerializeField] private Light pointLight;
    [SerializeField] private float lightRange = 6f;
    [SerializeField] private float lightIntensity = 7f;
    [SerializeField] private float lightDuration = 0.5f;

    private bool hasUnlocked = false;

    private void Update()
    {
        if (hasUnlocked) return;

        if (switch1.IsPressed && switch2.IsPressed && switch3.IsPressed)
        {
            UnlockWindows();
        }
    }

    private void UnlockWindows()
    {
        hasUnlocked = true;

        foreach (var window in windowsToUnlock)
        {
            if (window != null)
            {
                window.isLocked = false;

                // 개별 창문에 사운드와 빛 효과가 있다면 작동
                if (window.TryGetComponent(out AudioSource audio) && unlockSound != null)
                {
                    audio.PlayOneShot(unlockSound);
                }

                if (pointLight != null)
                    StartCoroutine(LightPulse()); // 공통 빛 효과
            }
        }
    }

    private System.Collections.IEnumerator LightPulse()
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
