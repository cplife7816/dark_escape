using UnityEngine;
using UnityEngine.Events;
using System;
using System.Reflection;

/// <summary>
/// "두꺼비집 스위치" 오브젝트에 붙여서 isElected 값을 저장/복원합니다.
/// sourceBehaviour의 public field/property 중 electedMemberName(기본: "isElected")를 찾아서 읽고/씁니다.
/// </summary>
[DisallowMultipleComponent]
public class BreakerSwitchSave : MonoBehaviour, ISaveable
{
    [Header("Source (holds isElected)")]
    [Tooltip("isElected를 보유한 컴포넌트(예: SwitchController 등)")]
    [SerializeField] private MonoBehaviour sourceBehaviour;

    [Tooltip("읽고/쓸 멤버 이름 (필드 혹은 프로퍼티). 대소문자 무시")]
    [SerializeField] private string electedMemberName = "isElected";

    [Header("Optional Broadcast on Restore")]
    [Tooltip("로드 시 값이 적용된 뒤 한 번 호출됩니다.")]
    [SerializeField] private UnityEvent<bool> onRestoredElectedChanged;

    [Serializable] private struct Data { public bool elected; public bool active; }

    public string CaptureState()
    {
        bool elected = TryGetBool(sourceBehaviour, electedMemberName, defaultValue: false);
        var d = new Data { elected = elected, active = gameObject.activeSelf };
        return JsonUtility.ToJson(d);
    }

    public void RestoreState(string json)
    {
        var d = JsonUtility.FromJson<Data>(json);
        gameObject.SetActive(d.active);

        TrySetBool(sourceBehaviour, electedMemberName, d.elected);
        onRestoredElectedChanged?.Invoke(d.elected);
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
        Debug.LogWarning($"[BreakerSwitchSave] '{name}' 멤버에 값을 쓸 수 없습니다.");
    }

    private static (FieldInfo, PropertyInfo) FindMember(Type t, string name)
    {
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        FieldInfo fi = null; PropertyInfo pi = null;
        // 대소문자 무시 검색
        foreach (var f in t.GetFields(flags))
            if (string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase)) { fi = f; break; }
        if (fi == null)
            foreach (var p in t.GetProperties(flags))
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) { pi = p; break; }
        return (fi, pi);
    }
}
