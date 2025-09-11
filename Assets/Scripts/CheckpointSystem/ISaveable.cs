using UnityEngine;

/// <summary>
/// Implement on components that can capture and restore their own state.
/// Keep the returned JSON small and self-contained (use your own DTO).
/// </summary>
public interface ISaveable
{
    /// <summary>Return a JSON string representing this component's state.</summary>
    string CaptureState();

    /// <summary>Restore from the JSON string produced by CaptureState().</summary>
    void RestoreState(string json);
}