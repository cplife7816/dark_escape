using UnityEngine;

public class PickupRotationOverride : MonoBehaviour
{
    [Tooltip("holdPosition에 적용될 localRotation")]
    public Vector3 customEulerRotation = Vector3.zero;

    [Tooltip("holdPosition에 적용될 위치 보정값")]
    public Vector3 offsetPosition = Vector3.zero;
}