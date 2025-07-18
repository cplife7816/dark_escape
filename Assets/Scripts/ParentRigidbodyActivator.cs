using UnityEngine;

public class ParentRigidbodyActivator : MonoBehaviour
{
    private bool hasActivated = false;

    private void Update()
    {
        if (!hasActivated && IsHeldByPlayer())
        {
            hasActivated = true;

            Transform root = transform.root;
            if (root.TryGetComponent(out Rigidbody rb))
            {
                rb.isKinematic = false;
                rb.useGravity = true;

                Debug.Log($"{name} → 부모 Rigidbody 활성화됨: {rb.name}");
            }
        }
    }

    private bool IsHeldByPlayer()
    {
        return transform.parent != null && transform.parent.name == "HoldPosition";
    }
}
