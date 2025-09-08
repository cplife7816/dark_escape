using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ElectricLeakIndicator : MonoBehaviour, IElectedReceiver
{
    [Header("Light")]
    [SerializeField] private Light pointLight;
    [SerializeField, Min(0.1f)] private float pulseUpDown = 0.5f; // 1�޽�(���/�ϰ�) �� �ð�
    [SerializeField] private float pulseRange = 6f;
    [SerializeField] private float pulseIntensity = 2.5f;
    [SerializeField] private float idleDelay = 0.25f; // �޽� ���� ����

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip electLeakLoop; // ���� ���� ���������� ����
    [SerializeField] private bool loopSound = true;

    [Header("Events (optional)")]
    public UnityEvent<bool> OnIndicatorActiveChanged;

    private bool active = false;
    private Coroutine pulseCo;

    public void SetElected(bool value)
    {
        if (active == value) return;
        active = value;

        if (active)
        {
            // ����
            if (audioSource)
            {
                audioSource.loop = loopSound;
                audioSource.clip = electLeakLoop;
                if (electLeakLoop) audioSource.Play();
            }

            // ����Ʈ �޽� ����
            if (pulseCo != null) StopCoroutine(pulseCo);
            pulseCo = StartCoroutine(CoPulseLoop());
        }
        else
        {
            // ����/����Ʈ ��� ����
            if (audioSource) audioSource.Stop();

            if (pulseCo != null) StopCoroutine(pulseCo);
            pulseCo = null;

            if (pointLight)
            {
                pointLight.range = 0f;
                pointLight.intensity = 0f;
            }
        }

        OnIndicatorActiveChanged?.Invoke(active);
    }

    private IEnumerator CoPulseLoop()
    {
        if (!pointLight) yield break;

        while (true)
        {
            // ���
            float t = 0f;
            float half = pulseUpDown * 0.5f;
            float startR = pointLight.range;
            float startI = pointLight.intensity;
            while (t < half)
            {
                t += Time.deltaTime;
                float k = t / half;
                pointLight.range = Mathf.Lerp(startR, pulseRange, k);
                pointLight.intensity = Mathf.Lerp(startI, pulseIntensity, k);
                yield return null;
            }

            // �ϰ�
            t = 0f; startR = pointLight.range; startI = pointLight.intensity;
            while (t < half)
            {
                t += Time.deltaTime;
                float k = t / half;
                pointLight.range = Mathf.Lerp(startR, 0f, k);
                pointLight.intensity = Mathf.Lerp(startI, 0f, k);
                yield return null;
            }

            // �ణ ��
            if (idleDelay > 0f) yield return new WaitForSeconds(idleDelay);
        }
    }
}
