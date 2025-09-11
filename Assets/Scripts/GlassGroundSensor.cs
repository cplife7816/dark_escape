using UnityEngine;

public class GlassGroundSensor : MonoBehaviour
{
    private GlassBreakController controller;

    public void Initialize(GlassBreakController c)
    {
        controller = c;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("FOOTSTEPS/ROCK") || other.gameObject.layer == LayerMask.NameToLayer("Default"))
        {
            controller?.OnGroundSensorTriggered(other);
        }
    }
}
