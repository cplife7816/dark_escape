using System.Collections;
using UnityEngine;

public class GlassShardCollisionHandler : MonoBehaviour
{
    private bool hasLanded = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (hasLanded) return;

        if (collision.gameObject.CompareTag("FOOTSTEPS/ROCK"))
        {
            hasLanded = true;
            StartCoroutine(DisablePhysicsAfterDelay());
        }
    }

    private IEnumerator DisablePhysicsAfterDelay()
    {
        yield return new WaitForSeconds(1f);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            Debug.Log($"[GlassShard] {name}: Rigidbody set to kinematic");
        }

        Collider[] colliders = GetComponents<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = false;
            Debug.Log($"[GlassShard] {name}: Collider ({col.GetType().Name}) disabled");
        }
    }
}
