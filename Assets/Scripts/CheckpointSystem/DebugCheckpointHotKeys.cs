using UnityEngine;

public class DebugCheckpointHotkeys : MonoBehaviour
{
    [SerializeField] string slotKey = "_Last";

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            SaveSystem.Instance.SaveCheckpoint(slotKey);
            Debug.Log($"[Hotkeys] Saved '{slotKey}'");
        }
        if (Input.GetKeyDown(KeyCode.F9))
        {
            SaveSystem.Instance.LoadCheckpoint(slotKey);
            Debug.Log($"[Hotkeys] Load requested '{slotKey}'");
        }
    }
}
