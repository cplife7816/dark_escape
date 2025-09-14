using UnityEngine;

[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public class KeyObject : MonoBehaviour
{
    private BoxCollider itemCollider;
    private Rigidbody itemRigidbody;

    private bool isReleased = false;

    private void Awake()
    {
        itemCollider = GetComponent<BoxCollider>();
        itemRigidbody = GetComponent<Rigidbody>();

        if (itemCollider == null || itemRigidbody == null)
        {
            Debug.LogError("[KeyObject] 필요한 컴포넌트(BoxCollider, Rigidbody)가 없습니다.");
            enabled = false;
            return;
        }

        itemCollider.enabled = false;
        itemRigidbody.isKinematic = true;
    }

    public void ReleaseFromGlass()
    {
        if (isReleased) return;
        isReleased = true;

        Debug.Log($"[KeyObject] {name} released from broken glass.");

        itemCollider.enabled = true;
        itemRigidbody.isKinematic = false;

        gameObject.tag = "Item";  // 플레이어가 줍기 가능하도록
        gameObject.layer = LayerMask.NameToLayer("Default");
    }
}
