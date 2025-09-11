using UnityEngine;

/// <summary>
/// 문/창문 등 '여닫이' 오브젝트의 상태를 SaveSystem 슬롯에 저장/복구.
/// 대상 스크립트는 아래 4개 API를 제공해야 함:
///   bool GetLocked();           void SetLocked(bool v);
///   float GetOpenRatio();       void SetOpenRatioImmediate(float t01);
///
/// Door.cs, WindowInteraction.cs 모두 위 API를 구현하면 이 하나로 통일 가능.
/// </summary>
[DisallowMultipleComponent]
public class OpenableSave : MonoBehaviour, ISaveable
{
    [System.Serializable]
    private class Data { public bool active; public bool locked; public float openRatio; }

    [Header("Target (Door or WindowInteraction)")]
    [SerializeField] private MonoBehaviour targetBehaviour; // Door 또는 WindowInteraction 참조
    [SerializeField] private bool applyActiveSelf = true;

    // 메서드 캐시
    private System.Func<bool> _getLocked;
    private System.Action<bool> _setLocked;
    private System.Func<float> _getOpenRatio;
    private System.Action<float> _setOpenImmediate;

    private void Awake()
    {
        if (!targetBehaviour)
        {
            Debug.LogError("[OpenableSave] targetBehaviour is null.");
            enabled = false;
            return;
        }

        var t = targetBehaviour.GetType();
        _getLocked = BindFunc<bool>(t, "GetLocked");
        _setLocked = BindAction<bool>(t, "SetLocked");
        _getOpenRatio = BindFunc<float>(t, "GetOpenRatio");
        _setOpenImmediate = BindAction<float>(t, "SetOpenRatioImmediate");
    }

    public string CaptureState()
    {
        float ratio = _getOpenRatio != null ? Mathf.Clamp01(_getOpenRatio()) : 0f;
        bool locked = _getLocked != null ? _getLocked() : false;

        var data = new Data
        {
            active = gameObject.activeSelf,
            locked = locked,
            openRatio = ratio
        };
        return JsonUtility.ToJson(data);
    }

    public void RestoreState(string json)
    {
        var d = JsonUtility.FromJson<Data>(json);
        if (applyActiveSelf) gameObject.SetActive(d.active);

        _setLocked?.Invoke(d.locked);
        _setOpenImmediate?.Invoke(Mathf.Clamp01(d.openRatio));
    }

    // ---- Helpers ----
    private System.Func<T> BindFunc<T>(System.Type t, string name)
    {
        var mi = t.GetMethod(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (mi == null) { Debug.LogWarning($"[OpenableSave] Missing method: {name}"); return null; }
        return (System.Func<T>)System.Delegate.CreateDelegate(typeof(System.Func<T>), targetBehaviour, mi, false);
    }
    private System.Action<T> BindAction<T>(System.Type t, string name)
    {
        var mi = t.GetMethod(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (mi == null) { Debug.LogWarning($"[OpenableSave] Missing method: {name}"); return null; }
        return (System.Action<T>)System.Delegate.CreateDelegate(typeof(System.Action<T>), targetBehaviour, mi, false);
    }
}
