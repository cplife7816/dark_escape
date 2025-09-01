using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ZoneDeactivateWalkersAndLockWindows_Lite : MonoBehaviour
{
    [Header("비활성화할 워커 루트들 (SetActive(false))")]
    public GameObject[] walkerRoots;

    [Header("잠글 문/창 루트들 (WindowInteraction이 붙어있는 오브젝트들)")]
    public GameObject[] windowRoots;

    [Header("플레이어 태그")]
    public string playerTag = "Player";

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        var mr = GetComponent<MeshRenderer>();
        if (mr) mr.enabled = false; // 투명 박스로 사용
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        // 1) 워커 루트 비활성화
        if (walkerRoots != null)
        {
            foreach (var root in walkerRoots)
                if (root) root.SetActive(false);
        }

        // 2) 문/창 닫고 잠그기 (WindowInteraction을 이름으로 찾아서 조작)
        if (windowRoots != null)
        {
            foreach (var wr in windowRoots)
            {
                if (!wr) continue;

                var wi = GetComponentByName(wr, "WindowInteraction"); // MonoBehaviour 인스턴스
                if (wi == null) continue;

                // private bool isOpen 읽기 시도 (없으면 false로 취급)
                bool isOpen = GetBoolField(wi, "isOpen", defaultValue: false);

                // 일단 잠금 해제 → 닫기 필요 시 Interact() 1회 → 다시 잠금
                SetBoolField(wi, "isLocked", false);
                if (isOpen)
                {
                    InvokeMethod(wi, "Interact");
                }
                SetBoolField(wi, "isLocked", true);
            }
        }
    }

    // --------- 리플렉션 유틸 ---------
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
            // 메서드가 없으면 SendMessage로도 시도 (있어도 매개변수 맞으면 호출)
            var mb = obj as MonoBehaviour;
            if (mb != null) mb.gameObject.SendMessage(methodName, SendMessageOptions.DontRequireReceiver);
        }
    }
}
