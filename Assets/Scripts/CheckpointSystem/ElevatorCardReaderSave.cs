using UnityEngine;
using UnityEngine.Events;
using System;
using System.Reflection;

/// <summary>
/// "카드키 리더" 오브젝트에 붙여서 isElevatorOn 값을 저장/복원합니다.
/// sourceBehaviour의 public field/property 중 memberName(기본: "isElevatorOn")을 찾아서 읽고/씁니다.
/// </summary>
[DisallowMultipleComponent]
public class ElevatorCardReaderSave : MonoBehaviour, ISaveable
{
    [Header("Source (holds isElevatorOn)")]
    [SerializeField] private MonoBehaviour sourceBehaviour;

    [Tooltip("읽고/쓸 멤버 이름 (필드 혹은 프로퍼티). 대소문자 무시")]
    [SerializeField] private string memberName = "isElevatorOn";

    [Header("Optional Broadcast on Restore")]
    [SerializeField] private UnityEvent<bool> onRestoredElevatorOnChanged;

    [Serializable] private struct Data { public bool on; public bool active; }

    public string CaptureState()
    {
        bool on = TryGetBool(sourceBehaviour, memberName, defaultValue: false);
        var d = new Data { on = on, active = gameObject.activeSelf };
        return JsonUtility.ToJson(d);
    }

    public void RestoreState(string json)
    {
        var d = JsonUtility.FromJson<Data>(json);
        gameObject.SetActive(d.active);

        TrySetBool(sourceBehaviour, memberName, d.on);
        onRestoredElevatorOnChanged?.Invoke(d.on);
    }

    // --------- Reflection helpers ----------
    private static bool TryGetBool(object obj, string name, bool defaultValue)
    {
        if (obj == null || string.IsNullOrEmpty(name)) return defaultValue;
        var (fi, pi) = FindMember(obj.GetType(), name);
        try
        {
            if (fi != null && fi.FieldType == typeof(bool)) return (bool)fi.GetValue(obj);
            if (pi != null && pi.PropertyType == typeof(bool)) return (bool)pi.GetValue(obj);
        }
        catch { }
        return defaultValue;
    }

    private static void TrySetBool(object obj, string name, bool value)
    {
        if (obj == null || string.IsNullOrEmpty(name)) return;
        var (fi, pi) = FindMember(obj.GetType(), name);
        try
        {
            if (fi != null && fi.FieldType == typeof(bool)) { fi.SetValue(obj, value); return; }
            if (pi != null && pi.PropertyType == typeof(bool) && pi.CanWrite) { pi.SetValue(obj, value); return; }
        }
        catch { }
        Debug.LogWarning($"[ElevatorCardReaderSave] '{name}' 멤버에 값을 쓸 수 없습니다.");
    }

    private static (FieldInfo, PropertyInfo) FindMember(Type t, string name)
    {
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        FieldInfo fi = null; PropertyInfo pi = null;
        foreach (var f in t.GetFields(flags))
            if (string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase)) { fi = f; break; }
        if (fi == null)
            foreach (var p in t.GetProperties(flags))
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) { pi = p; break; }
        return (fi, pi);
    }
}
