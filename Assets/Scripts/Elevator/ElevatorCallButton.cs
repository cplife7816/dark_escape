// ElevatorCallButton.cs
using UnityEngine;

public class ElevatorCallButton : MonoBehaviour, ITryInteractable
{
    [SerializeField] private ElevatorController controller;
    [SerializeField] private ElevatorFloor floor = ElevatorFloor.BF;
    [SerializeField] private bool debugLogs = true;

    public void TryInteract()
    {
        if (debugLogs) Debug.Log($"[ELEV/BUTTON] Call '{name}' TryInteract ¡æ {floor}");
        if (!controller) return;
        controller.CallElevator(floor);
    }
}
