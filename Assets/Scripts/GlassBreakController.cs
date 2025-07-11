using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class GlassBreakController : MonoBehaviour
{
    [Header("Glass References")]
    [SerializeField] private GameObject intactGlass;
    [SerializeField] private GameObject shatteredGlass;

    [Header("Sound Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip breakSound;

    [Header("Material Settings")]
    [SerializeField] private string targetMaterialName = "bottle";
    [SerializeField] private Color targetColor = Color.red;
    [SerializeField] private float colorChangeDuration = 3f;

    [Header("Light Pulse")]
    [SerializeField] private Light pointLight;
    [SerializeField] private float lightRange = 5f;
    [SerializeField] private float lightIntensity = 7f;
    [SerializeField] private float lightDuration = 0.5f;

    private static bool colorChangedOnce = false;
    private Rigidbody rb;
    private bool isBroken = false;
    private bool wasHeldByPlayer = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null && CompareTag("Item"))
            rb.isKinematic = true;

        if (pointLight != null)
        {
            pointLight.range = 0f;
            pointLight.intensity = 0f;
        }
    }

    private void Update()
    {
        if (wasHeldByPlayer && rb != null && rb.isKinematic && transform.parent == null)
        {
            rb.isKinematic = false;
            wasHeldByPlayer = false;
        }
    }

    public void NotifyDroppedByPlayer()
    {
        wasHeldByPlayer = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isBroken) return;
        isBroken = true;

        if (pointLight != null)
        {
            pointLight.enabled = true; // 혹시 꺼져 있으면 켜기

            if (LightPulseController.Instance != null)
            {
                LightPulseController.Instance.TriggerPulse(pointLight, lightRange, lightIntensity, lightDuration, this);
            }
            else
            {
                // fallback: 직접 pulsing
                StartCoroutine(SimpleLightPulse(pointLight, lightRange, lightIntensity, lightDuration));
            }
        }

        BreakGlass();

        if (audioSource != null && breakSound != null)
            audioSource.PlayOneShot(breakSound);

        if (pointLight != null && LightPulseController.Instance != null)
            LightPulseController.Instance.TriggerPulse(pointLight, lightRange, lightIntensity, lightDuration, this);

        float changeDuration = colorChangedOnce ? 0.5f : colorChangeDuration;
        StartCoroutine(ChangeWhiteMaterialsOnly(changeDuration));
        StartCoroutine(FadeOutFragments());

        colorChangedOnce = true;
    }

    private void BreakGlass()
    {
        if (intactGlass != null)
            intactGlass.SetActive(false);
        if (shatteredGlass != null)
            shatteredGlass.SetActive(true);
    }

    private IEnumerator ChangeWhiteMaterialsOnly(float duration)
    {
        List<Material> affectedMaterials = new List<Material>();

        Renderer[] allRenderers = FindObjectsOfType<Renderer>();
        foreach (Renderer r in allRenderers)
        {
            foreach (Material mat in r.materials)
            {
                if (mat.name.StartsWith(targetMaterialName) && IsExactlyWhite(mat.color))
                {
                    affectedMaterials.Add(mat);
                }
            }
        }

        if (shatteredGlass != null)
        {
            Renderer[] hiddenRenderers = shatteredGlass.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in hiddenRenderers)
            {
                foreach (Material mat in r.materials)
                {
                    if (mat.name.StartsWith(targetMaterialName) && IsExactlyWhite(mat.color) && !affectedMaterials.Contains(mat))
                    {
                        affectedMaterials.Add(mat);
                    }
                }
            }
        }

        if (affectedMaterials.Count == 0)
            yield break;

        float elapsed = 0f;
        Dictionary<Material, Color> originalColors = new Dictionary<Material, Color>();
        foreach (var mat in affectedMaterials)
            originalColors[mat] = mat.color;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            foreach (var mat in affectedMaterials)
            {
                mat.color = Color.Lerp(originalColors[mat], targetColor, t);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (var mat in affectedMaterials)
        {
            mat.color = targetColor;
        }
    }

    private bool IsExactlyWhite(Color c)
    {
        return Mathf.Approximately(c.r, 1f) && Mathf.Approximately(c.g, 1f) && Mathf.Approximately(c.b, 1f);
    }

    private IEnumerator FadeOutFragments()
    {
        yield return new WaitForSeconds(10f);

        Renderer[] renderers = shatteredGlass.GetComponentsInChildren<Renderer>(true);
        List<Material> materials = new List<Material>();
        foreach (var r in renderers)
            materials.AddRange(r.materials);

        float fadeDuration = 2f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            foreach (var mat in materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color c = mat.color;
                    c.a = alpha;
                    mat.color = c;
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 🔥 여기! red_glass 자체 비활성화
        gameObject.SetActive(false);
    }

    private IEnumerator SimpleLightPulse(Light light, float maxRange, float intensity, float duration)
    {
        float half = duration / 2f;
        float t = 0f;

        while (t < half)
        {
            t += Time.deltaTime;
            float ratio = t / half;
            light.range = Mathf.Lerp(0f, maxRange, ratio);
            light.intensity = Mathf.Lerp(0f, intensity, ratio);
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float ratio = t / half;
            light.range = Mathf.Lerp(maxRange, 0f, ratio);
            light.intensity = Mathf.Lerp(intensity, 0f, ratio);
            yield return null;
        }

        light.range = 0f;
        light.intensity = 0f;
    }
}
