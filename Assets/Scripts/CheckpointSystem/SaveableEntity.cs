using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Put this on any GameObject that owns one or more ISaveable components.
/// Provides a stable unique Id for checkpoint lookup.
/// </summary>
[ExecuteAlways]
public class SaveableEntity : MonoBehaviour
{
    [SerializeField] private string id;
    public string Id => id;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Do not assign IDs on prefab assets — only on scene instances.
        if (EditorUtility.IsPersistent(this)) return; // prefab asset
        if (string.IsNullOrEmpty(id))
        {
            id = System.Guid.NewGuid().ToString();
            EditorUtility.SetDirty(this);
        }
    }
#endif
}