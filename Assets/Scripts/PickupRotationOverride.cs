using UnityEngine;

public class PickupRotationOverride : MonoBehaviour
{
    [Tooltip("holdPosition�� ����� localRotation")]
    public Vector3 customEulerRotation = Vector3.zero;

    [Tooltip("holdPosition�� ����� ��ġ ������")]
    public Vector3 offsetPosition = Vector3.zero;
}