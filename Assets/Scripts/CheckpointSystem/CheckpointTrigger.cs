using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Simple trigger to save the _Last checkpoint or a named anchor.
/// </summary>
public class CheckpointTrigger : MonoBehaviour
{
    [SerializeField] private bool saveOnEnter = true;
    [SerializeField] private string slotKey = "_Last";
    [SerializeField] private string requiredTag = "Player";
    [SerializeField] private UnityEvent onSaved;

    private void OnTriggerEnter(Collider other)
    {
        if (!saveOnEnter) return;
        if (!other.CompareTag(requiredTag)) return;
        Save();
    }

    public void Save()
    {
        if (string.IsNullOrWhiteSpace(slotKey)) slotKey = "_Last";
        SaveSystem.Instance.SaveCheckpoint(slotKey);
        onSaved?.Invoke();
    }
}