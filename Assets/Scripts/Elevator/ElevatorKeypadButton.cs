// ElevatorKeypadButton.cs
using UnityEngine;

public class ElevatorKeypadButton : MonoBehaviour, ITryInteractable
{
    [SerializeField] private ElevatorController controller;
    [SerializeField] private ElevatorFloor targetFloor = ElevatorFloor.BF;
    [SerializeField] private bool debugLogs = true;

    public void TryInteract()
    {
        if (debugLogs) Debug.Log($"[ELEV/BUTTON] Keypad '{name}' TryInteract ¡æ {targetFloor}");
        if (!controller) return;
        controller.RequestFloor(targetFloor);
    }
}
