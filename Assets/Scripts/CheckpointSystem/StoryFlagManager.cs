using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Global scenario flags (branch conditions). Add this once in the scene (or via prefab in bootstrap scene).
/// </summary>
public class StoryFlagManager : MonoBehaviour, ISaveable
{
    public static StoryFlagManager I { get; private set; }
    private HashSet<string> flags = new HashSet<string>();

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Set(string flag) => flags.Add(flag);
    public bool Has(string flag) => flags.Contains(flag);
    public void Clear(string flag) => flags.Remove(flag);

    [System.Serializable] class Data { public string[] list; }

    public string CaptureState()
    {
        var d = new Data { list = flags.ToArray() };
        return JsonUtility.ToJson(d);
    }

    public void RestoreState(string json)
    {
        var d = JsonUtility.FromJson<Data>(json);
        flags = new HashSet<string>(d?.list ?? new string[0]);
    }
}