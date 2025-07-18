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

                Debug.Log($"{name} �� �θ� Rigidbody Ȱ��ȭ��: {rb.name}");
            }
        }
    }

    private bool IsHeldByPlayer()
    {
        return transform.parent != null && transform.parent.name == "HoldPosition";
    }
}
