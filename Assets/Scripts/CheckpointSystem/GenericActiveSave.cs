using UnityEngine;

/// <summary>
/// Saves GameObject activeSelf. Attach to any GameObject that may toggle on/off by gimmicks.
/// </summary>
[DisallowMultipleComponent]
public class GenericActiveSave : MonoBehaviour, ISaveable
{
    [System.Serializable] class Data { public bool active; }

    public string CaptureState()
    {
        return JsonUtility.ToJson(new Data { active = gameObject.activeSelf });
    }

    public void RestoreState(string json)
    {
        var d = JsonUtility.FromJson<Data>(json);
        if (d != null) gameObject.SetActive(d.active);
    }
}