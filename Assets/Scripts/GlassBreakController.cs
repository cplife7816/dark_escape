using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class GlassBreakController : MonoBehaviour, IDroppable
{
    [SerializeField] private float pulseUpRatio = 0.1f;
    [SerializeField] private float pulseHoldRatio = 0.5f;
    [SerializeField] private float pulseDownRatio = 0.4f;
    [SerializeField] private string subtitleName = "bottle";

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

    [Header("Ground Sensor")]
    [SerializeField] private GameObject groundSensorObject;

    private static HashSet<string> colorChangedSet = new HashSet<string>();
    private Rigidbody rb;
    private bool isBroken = false;
    private BoxCollider mainCollider;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        mainCollider = GetComponent<BoxCollider>();

        if (rb != null && CompareTag("Item"))
            rb.isKinematic = true;

        if (pointLight != null)
        {
            pointLight.range = 0f;
            pointLight.intensity = 0f; // 유지
            pointLight.enabled = true; // 항상 켜져 있어야 효과 반영됨
        }

        if (groundSensorObject != null)
        {
            groundSensorObject.SetActive(false); // ⛔ 처음엔 꺼둠

            var sensorScript = groundSensorObject.AddComponent<GlassGroundSensor>();
            sensorScript.Initialize(this);
        }
    }

    public void Dropped()
    {
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        if (mainCollider != null)
            mainCollider.enabled = true;

        if (groundSensorObject != null)
            groundSensorObject.SetActive(true);

        // ✅ 플레이어가 다시 줍지 못하도록 Item 태그 제거
        if (CompareTag("Item"))
        {
            gameObject.tag = "Untagged";
            Debug.Log("[GlassBreak] Dropped → Item 태그 제거됨");
        }
    }

    private void OnTransformParentChanged()
    {
        if (transform.parent != null && transform.parent.name == "HoldPosition")
        {
            if (mainCollider != null)
                mainCollider.enabled = false;
        }
    }

    private float groundYFromSensor = float.NegativeInfinity;

    public void OnGroundSensorTriggered(Collider other)
    {
        if (isBroken) return;
        // 센서가 닿은 지면의 Y를 추정: 콜라이더 경계 상단/하단 중 상황에 맞게 선택
        groundYFromSensor = other.bounds.max.y; // 또는 .min.y, 환경에 맞게
        isBroken = true;
        StartCoroutine(DelayedBreak());
    }

    private IEnumerator DelayedBreak()
    {
        yield return null;

        // ★ 파손 직후, 파편들에게 지면 Y 주입
        if (shatteredGlass != null)
        {
            var handlers = shatteredGlass.GetComponentsInChildren<GlassShardCollisionHandler>(true);
            foreach (var h in handlers) h.SetGroundY(groundYFromSensor);
        }

        BreakGlass();
        TriggerLight();
        PlayBreakSound();
        float duration = colorChangedSet.Contains(targetMaterialName) ? 0.5f : colorChangeDuration;
        StartCoroutine(ChangeWhiteMaterialsOnly(duration));
        colorChangedSet.Add(targetMaterialName);

        StartCoroutine(DisableCollisionsAfterBreak()); // 아래에서 게이트 체크
        ApplyNameToMatchingObjects();
    }

    private void BreakGlass()
    {
        if (intactGlass != null)
            intactGlass.SetActive(false);
        if (shatteredGlass != null)
            shatteredGlass.SetActive(true);

        if (groundSensorObject != null)
            groundSensorObject.SetActive(false); // ✅ 더 이상 감지하지 않도록
        ApplyEffectToPlayer();

        foreach (var keyObj in GetComponentsInChildren<KeyObject>(true))
        {
            keyObj.ReleaseFromGlass();
        }
    }

    private void TriggerLight()
    {
        if (pointLight == null) return;

        StopAllCoroutines(); // 중복 방지
        StartCoroutine(PulseLightWithRatios());
    }

    private void PlayBreakSound()
    {
        if (audioSource != null && breakSound != null)
        {
            audioSource.PlayOneShot(breakSound);
        }
    }

    private IEnumerator ChangeWhiteMaterialsOnly(float duration)
    {
        List<Material> targets = new List<Material>();
        Renderer[] renderers = FindObjectsOfType<Renderer>();

        foreach (Renderer r in renderers)
        {
            foreach (Material mat in r.materials)
            {
                if (mat.name.StartsWith(targetMaterialName) && IsExactlyWhite(mat.color))
                    targets.Add(mat);
            }
        }

        if (shatteredGlass != null)
        {
            Renderer[] hidden = shatteredGlass.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in hidden)
            {
                foreach (Material mat in r.materials)
                {
                    if (mat.name.StartsWith(targetMaterialName) && IsExactlyWhite(mat.color) && !targets.Contains(mat))
                        targets.Add(mat);
                }
            }
        }

        if (targets.Count == 0) yield break;

        float elapsed = 0f;
        Dictionary<Material, Color> origin = new();
        foreach (var mat in targets) origin[mat] = mat.color;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            foreach (var mat in targets)
                mat.color = Color.Lerp(origin[mat], targetColor, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (var mat in targets)
            mat.color = targetColor;
    }

    private bool IsExactlyWhite(Color c)
    {
        return Mathf.Approximately(c.r, 1f) &&
               Mathf.Approximately(c.g, 1f) &&
               Mathf.Approximately(c.b, 1f);
    }

    private IEnumerator DisableCollisionsAfterBreak()
    {
        yield return new WaitForSeconds(2f); // 기존 유지 (필요시 값만 늘려도 OK)

        if (shatteredGlass != null)
        {
            // 1) 파편 Rigidbody는 '준비된 것만' kinematic
            Rigidbody[] rigidbodies = shatteredGlass.GetComponentsInChildren<Rigidbody>(true);
            foreach (var r in rigidbodies)
            {
                var gate = r.GetComponent<GlassShardCollisionHandler>();
                if (gate != null && !gate.ReadyToDisableNow())
                    continue; // 아직 지면Y 미도달 → 패스

                r.isKinematic = true;
            }

            // 2) 파편 Collider도 '준비된 것만' 비활성화
            Collider[] colliders = shatteredGlass.GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders)
            {
                var gate = col.GetComponent<GlassShardCollisionHandler>();
                if (gate != null && !gate.ReadyToDisableNow())
                    continue;

                col.enabled = false;
            }
        }

        if (mainCollider != null) mainCollider.enabled = false;
        if (rb != null) rb.isKinematic = true;
    }

    private void ApplyNameToMatchingObjects()
    {
        string newName = subtitleName;
        if (string.IsNullOrEmpty(newName) || newName == "???") return;

        Renderer thisRenderer = GetComponentInChildren<Renderer>(true);
        if (thisRenderer == null || thisRenderer.sharedMaterial == null) return;

        string targetMaterialBaseName = thisRenderer.sharedMaterial.name.Replace(" (Instance)", "");

        GlassBreakController[] allGlass = FindObjectsOfType<GlassBreakController>();
        foreach (var glass in allGlass)
        {
            Renderer otherRenderer = glass.GetComponentInChildren<Renderer>(true);
            if (otherRenderer == null || otherRenderer.sharedMaterial == null) continue;

            string otherMatName = otherRenderer.sharedMaterial.name.Replace(" (Instance)", "");

            if (otherMatName == targetMaterialBaseName)
            {
                glass.gameObject.name = newName;

                SubtitleObject subtitle = glass.GetComponent<SubtitleObject>();
                if (subtitle != null)
                {
                    var subtitleField = typeof(SubtitleObject)
                        .GetField("subtitleText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (subtitleField != null)
                        subtitleField.SetValue(subtitle, newName);
                }
            }
        }
    }

    private void ApplyEffectToPlayer()
    {
        FirstPersonController player = FindObjectOfType<FirstPersonController>();
        if (player == null) return;

        player.ApplyGlassEffect(targetMaterialName); // 추출 없이 그대로 전달
    }

    private IEnumerator PulseLightWithRatios()
    {
        float total = pulseUpRatio + pulseHoldRatio + pulseDownRatio;
        float upTime = lightDuration * (pulseUpRatio / total);
        float holdTime = lightDuration * (pulseHoldRatio / total);
        float downTime = lightDuration * (pulseDownRatio / total);

        float timer = 0f;

        // 커지기
        while (timer < upTime)
        {
            timer += Time.deltaTime;
            float t = timer / upTime;
            pointLight.range = Mathf.Lerp(0f, lightRange, t);
            pointLight.intensity = Mathf.Lerp(0f, lightIntensity, t);
            yield return null;
        }

        // 유지
        pointLight.range = lightRange;
        pointLight.intensity = lightIntensity;
        yield return new WaitForSeconds(holdTime);

        // 줄어들기
        timer = 0f;
        while (timer < downTime)
        {
            timer += Time.deltaTime;
            float t = timer / downTime;
            pointLight.range = Mathf.Lerp(lightRange, 0f, t);
            pointLight.intensity = Mathf.Lerp(lightIntensity, 0f, t);
            yield return null;
        }

        pointLight.range = 0f;
        pointLight.intensity = 0f;
    }

}
