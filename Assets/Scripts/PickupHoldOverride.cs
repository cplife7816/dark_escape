using UnityEngine;

public class PickupHoldOverride : MonoBehaviour
{
    [Tooltip("손에 들렸을 때 offset 위치")]
    public Vector3 holdOffset = Vector3.zero;

    public Vector3 GetOffset() => holdOffset;
}
