using UnityEngine;

public class PickupHoldOverride : MonoBehaviour
{
    [Tooltip("�տ� ����� �� offset ��ġ")]
    public Vector3 holdOffset = Vector3.zero;

    public Vector3 GetOffset() => holdOffset;
}
