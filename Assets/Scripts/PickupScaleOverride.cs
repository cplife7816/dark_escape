using UnityEngine;

public class PickupScaleOverride : MonoBehaviour
{
    [Header("Scale Multipliers")]
    public float heldScaleMultiplier = 1.0f;
    public float droppedScaleMultiplier = 1.5f;

    private Vector3 originalWorldScale;
    private bool initialized = false;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        if (!IsHeldByPlayer())
        {
            ApplyDroppedScale();
        }
    }

    private void Initialize()
    {
        if (!initialized)
        {
            originalWorldScale = transform.lossyScale;
            initialized = true;
        }
    }

    public void ApplyHeldScale()
    {
        Initialize();

        Vector3 targetWorldScale = originalWorldScale * heldScaleMultiplier;
        SetLocalScaleToMatchWorld(targetWorldScale);
    }

    public void ApplyDroppedScale()
    {
        Initialize();

        Vector3 targetWorldScale = originalWorldScale * droppedScaleMultiplier;
        SetLocalScaleToMatchWorld(targetWorldScale);
    }

    private void SetLocalScaleToMatchWorld(Vector3 desiredWorldScale)
    {
        Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;

        transform.localScale = new Vector3(
            desiredWorldScale.x / parentScale.x,
            desiredWorldScale.y / parentScale.y,
            desiredWorldScale.z / parentScale.z
        );
    }

    private bool IsHeldByPlayer()
    {
        return transform.parent != null && transform.parent.CompareTag("MainCamera");
    }
}
