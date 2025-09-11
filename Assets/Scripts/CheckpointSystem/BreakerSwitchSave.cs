using UnityEngine;
using UnityEngine.Events;
using System;
using System.Reflection;

/// <summary>
/// "�β����� ����ġ" ������Ʈ�� �ٿ��� isElected ���� ����/�����մϴ�.
/// sourceBehaviour�� public field/property �� electedMemberName(�⺻: "isElected")�� ã�Ƽ� �а�/���ϴ�.
/// </summary>
[DisallowMultipleComponent]
public class BreakerSwitchSave : MonoBehaviour, ISaveable
{
    [Header("Source (holds isElected)")]
    [Tooltip("isElected�� ������ ������Ʈ(��: SwitchController ��)")]
    [SerializeField] private MonoBehaviour sourceBehaviour;

    [Tooltip("�а�/�� ��� �̸� (�ʵ� Ȥ�� ������Ƽ). ��ҹ��� ����")]
    [SerializeField] private string electedMemberName = "isElected";

    [Header("Optional Broadcast on Restore")]
    [Tooltip("�ε� �� ���� ����� �� �� �� ȣ��˴ϴ�.")]
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
        Debug.LogWarning($"[BreakerSwitchSave] '{name}' ����� ���� �� �� �����ϴ�.");
    }

    private static (FieldInfo, PropertyInfo) FindMember(Type t, string name)
    {
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        FieldInfo fi = null; PropertyInfo pi = null;
        // ��ҹ��� ���� �˻�
        foreach (var f in t.GetFields(flags))
            if (string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase)) { fi = f; break; }
        if (fi == null)
            foreach (var p in t.GetProperties(flags))
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) { pi = p; break; }
        return (fi, pi);
    }
}
