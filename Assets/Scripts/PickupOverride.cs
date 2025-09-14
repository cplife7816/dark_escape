using UnityEngine;

/// <summary>
/// 통합 Pickup 설정 스크립트: 회전, 위치 보정, 스케일 적용을 하나로 관리합니다.
/// </summary>
public class PickupOverride : MonoBehaviour
{
    [Header("Hold Position Offset")]
    [Tooltip("손에 들렸을 때 offset 위치 (기존 PickupHoldOverride)")]
    public Vector3 holdOffset = Vector3.zero;

    [Header("Hold Rotation")]
    [Tooltip("손에 들렸을 때 회전 (기존 PickupRotationOverride)")]
    public Vector3 customEulerRotation = Vector3.zero;

    [Header("Scale Settings")]
    [Tooltip("손에 들렸을 때 월드 스케일 비율 (기존 PickupScaleOverride)")]
    public float heldScaleMultiplier = 1.0f;
    [Tooltip("놓았을 때 월드 스케일 비율 (기존 PickupScaleOverride)")]
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

    // Getter for integration
    public Vector3 GetHoldOffset() => holdOffset;
    public Vector3 GetRotation() => customEulerRotation;
}
