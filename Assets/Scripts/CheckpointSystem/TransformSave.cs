using UnityEngine;

/// <summary>
/// Saves Transform position & rotation (world). Safe for Player spawn points or movable objects.
/// </summary>
[DisallowMultipleComponent]
public class TransformSave : MonoBehaviour, ISaveable
{
    [System.Serializable] class Data { public Vector3 pos; public Quaternion rot; }
    [SerializeField] private Transform target;

    private void Reset() { target = transform; }

    public string CaptureState()
    {
        var t = target != null ? target : transform;
        return JsonUtility.ToJson(new Data { pos = t.position, rot = t.rotation });
    }

    public void RestoreState(string json)
    {
        var d = JsonUtility.FromJson<Data>(json);
        var t = target != null ? target : transform;
        if (d != null) t.SetPositionAndRotation(d.pos, d.rot);
    }
}