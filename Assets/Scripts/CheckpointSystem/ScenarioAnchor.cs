using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Place this where you want to mark a "scenario anchor". You can save and load that anchor by name.
/// Useful for chapter checkpoints or QA jump points.
/// </summary>
public class ScenarioAnchor : MonoBehaviour
{
    [SerializeField] private string anchorKey = "Chapter2_LabA";
    [SerializeField] private bool saveOnEnter = false;
    [SerializeField] private bool loadOnEnter = false;
    [SerializeField] private string requiredTag = "Player";
    [SerializeField] private UnityEvent onSaved;
    [SerializeField] private UnityEvent onLoaded;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(requiredTag)) return;
        if (saveOnEnter) { SaveNow(); }
        if (loadOnEnter) { LoadNow(); }
    }

    public void SaveNow()
    {
        SaveSystem.Instance.SaveCheckpoint(anchorKey);
        onSaved?.Invoke();
    }

    public void LoadNow()
    {
        SaveSystem.Instance.LoadCheckpoint(anchorKey);
        onLoaded?.Invoke();
    }
}