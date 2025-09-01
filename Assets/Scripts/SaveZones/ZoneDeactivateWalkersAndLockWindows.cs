using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ZoneDeactivateWalkersAndLockWindows_Lite : MonoBehaviour
{
    [Header("��Ȱ��ȭ�� ��Ŀ ��Ʈ�� (SetActive(false))")]
    public GameObject[] walkerRoots;

    [Header("��� ��/â ��Ʈ�� (WindowInteraction�� �پ��ִ� ������Ʈ��)")]
    public GameObject[] windowRoots;

    [Header("�÷��̾� �±�")]
    public string playerTag = "Player";

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        var mr = GetComponent<MeshRenderer>();
        if (mr) mr.enabled = false; // ���� �ڽ��� ���
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        // 1) ��Ŀ ��Ʈ ��Ȱ��ȭ
        if (walkerRoots != null)
        {
            foreach (var root in walkerRoots)
                if (root) root.SetActive(false);
        }

        // 2) ��/â �ݰ� ��ױ� (WindowInteraction�� �̸����� ã�Ƽ� ����)
        if (windowRoots != null)
        {
            foreach (var wr in windowRoots)
            {
                if (!wr) continue;

                var wi = GetComponentByName(wr, "WindowInteraction"); // MonoBehaviour �ν��Ͻ�
                if (wi == null) continue;

                // private bool isOpen �б� �õ� (������ false�� ���)
                bool isOpen = GetBoolField(wi, "isOpen", defaultValue: false);

                // �ϴ� ��� ���� �� �ݱ� �ʿ� �� Interact() 1ȸ �� �ٽ� ���
                SetBoolField(wi, "isLocked", false);
                if (isOpen)
                {
                    InvokeMethod(wi, "Interact");
                }
                SetBoolField(wi, "isLocked", true);
            }
        }
    }

    // --------- ���÷��� ��ƿ ---------
    private static MonoBehaviour GetComponentByName(GameObject go, string typeName)
    {
        var all = go.GetComponents<MonoBehaviour>();
        foreach (var c in all)
        {
            if (c == null) continue;
            if (c.GetType().Name == typeName) return c;
        }
        return null;
    }

    private static bool GetBoolField(object obj, string fieldName, bool defaultValue)
    {
        var f = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(bool))
        {
            try { return (bool)f.GetValue(obj); } catch { }
        }
        return defaultValue;
    }

    private static void SetBoolField(object obj, string fieldName, bool value)
    {
        var f = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(bool))
        {
            try { f.SetValue(obj, value); } catch { }
        }
    }

    private static void InvokeMethod(object obj, string methodName)
    {
        var m = obj.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (m != null && m.GetParameters().Length == 0)
        {
            try { m.Invoke(obj, null); } catch { }
        }
        else
        {
            // �޼��尡 ������ SendMessage�ε� �õ� (�־ �Ű����� ������ ȣ��)
            var mb = obj as MonoBehaviour;
            if (mb != null) mb.gameObject.SendMessage(methodName, SendMessageOptions.DontRequireReceiver);
        }
    }
}
