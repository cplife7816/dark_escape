using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// [���� �ڽ� 2����]
/// - Player�� Ʈ���ſ� �����ϸ�, ������ Walker���� ��Ȱ��ȭ�ϰ�
///   ���� �ݰ� ��ٴ�.
/// - �� ���� �켱����:
///   1) doorObject�� ���� SendMessage("Close") / SendMessage("Lock") �õ�
///   2) doorObject�� ������Ʈ���� IsLocked/locked ���� bool �ʵ� true�� ���� �õ�
///   3) (�ʿ� ��) onDoorCloseAndLock �̺�Ʈ�� ���� �޼��� ����
/// </summary>
[RequireComponent(typeof(Collider))]
public class ZoneWalkerDeactivatorAndDoorLocker : MonoBehaviour
{
    [Header("�÷��̾� ����")]
    public bool usePlayerTag = true;
    public string playerTag = "Player";

    [Header("��Ȱ��ȭ ��� (�� �� �ʿ��� �͸� ä�켼��)")]
    [Tooltip("��Ȱ��ȭ�� Walker ��Ʈ ���ӿ�����Ʈ�� (SetActive(false))")]
    public List<GameObject> walkerRootsToDeactivate = new List<GameObject>();

    [Tooltip("��Ȱ��ȭ�� Walker ���� ������Ʈ�� (enabled = false)")]
    public List<Behaviour> walkerBehavioursToDisable = new List<Behaviour>();

    [Header("�� ����")]
    [Tooltip("�ݰ� ��� �� ������Ʈ(�� ��Ʈ �Ǵ� Door ��ũ��Ʈ�� ���� ������Ʈ)")]
    public GameObject doorObject;

    [Tooltip("�� �ݱ� �޽��� �̸� (������Ʈ �� ��ũ��Ʈ �޼���� ���� ���� ����)")]
    public string closeMethodName = "Close";

    [Tooltip("�� ��� �޽��� �̸� (������Ʈ �� ��ũ��Ʈ �޼���� ���� ���� ����)")]
    public string lockMethodName = "Lock";

    [Tooltip("��� ���·� �����Ǵ� �ʵ� �ĺ���(������� Ž��)")]
    public List<string> lockFieldCandidates = new List<string> { "IsLocked", "isLocked", "Locked", "locked" };

    [Header("�̺�Ʈ(����)")]
    [Tooltip("Walker ��Ȱ��ȭ ���� ȣ��(����/����Ʈ ��)")]
    public UnityEvent onDeactivated;

    [Tooltip("�� �ݱ�/��� ���Ŀ� �߰��� ȣ���� �̺�Ʈ(�ִϸ��̼� Ʈ���� �� ���� ����)")]
    public UnityEvent onDoorCloseAndLock;

    [Header("Ʈ���� 1ȸ�� ó��")]
    public bool deactivateThisObjectAfterTrigger = true;
    public float selfDeactivateDelay = 0f;

    private bool _hasTriggered = false;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        var mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasTriggered) return;
        if (!IsPlayer(other)) return;

        _hasTriggered = true;

        DeactivateWalkers();
        onDeactivated?.Invoke();

        CloseAndLockDoor();
        onDoorCloseAndLock?.Invoke();

        if (deactivateThisObjectAfterTrigger)
        {
            if (selfDeactivateDelay <= 0f) gameObject.SetActive(false);
            else Invoke(nameof(DeactivateSelf), selfDeactivateDelay);
        }
    }

    private bool IsPlayer(Collider other)
    {
        if (usePlayerTag) return other.CompareTag(playerTag);
        return other.GetComponentInParent<FirstPersonController>() != null;
    }

    private void DeactivateWalkers()
    {
        foreach (var go in walkerRootsToDeactivate)
        {
            if (go == null) continue;
            if (go.activeSelf) go.SetActive(false);
        }

        foreach (var b in walkerBehavioursToDisable)
        {
            if (b == null) continue;
            b.enabled = false;
        }

        Debug.Log("[ZoneWalkerDeactivatorAndDoorLocker] ������ Walker���� ��Ȱ��ȭ�߽��ϴ�.");
    }

    private void CloseAndLockDoor()
    {
        if (doorObject == null)
        {
            Debug.LogWarning("[ZoneWalkerDeactivatorAndDoorLocker] doorObject�� �������� �ʾҽ��ϴ�.");
            return;
        }

        // 1) �޼��� ȣ�� �õ� (��� ���� ����)
        //    - SendMessage�� ���� ������Ʈ���� ��ε�ĳ��Ʈ���� �����Ƿ�,
        //      Door ��Ʈ�� �پ��ִٰ� �����ϰų�, �ʿ��� ��� doorObject�� Door ������Ʈ�� ���� ������Ʈ�� ���� �����ϼ���.
        doorObject.SendMessage(closeMethodName, SendMessageOptions.DontRequireReceiver);
        doorObject.SendMessage(lockMethodName, SendMessageOptions.DontRequireReceiver);

        // 2) bool ��� �ʵ� ���� �õ� (���÷���)
        TrySetLockFieldTrue(doorObject);

        Debug.Log("[ZoneWalkerDeactivatorAndDoorLocker] �� �ݱ�/��� ������ �õ��߽��ϴ�.");
    }

    private void TrySetLockFieldTrue(GameObject target)
    {
        var components = target.GetComponents<Component>();
        foreach (var comp in components)
        {
            if (comp == null) continue;
            var type = comp.GetType();
            // public / non-public ��� Ž��
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            foreach (var fieldName in lockFieldCandidates)
            {
                var f = type.GetField(fieldName, flags);
                if (f != null && f.FieldType == typeof(bool))
                {
                    try
                    {
                        f.SetValue(comp, true);
                        Debug.Log($"[ZoneWalkerDeactivatorAndDoorLocker] {type.Name}.{fieldName} = true �� ����");
                        return;
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[ZoneWalkerDeactivatorAndDoorLocker] {type.Name}.{fieldName} ���� ����: {e.Message}");
                    }
                }
            }
        }
    }

    private void DeactivateSelf()
    {
        gameObject.SetActive(false);
    }
}
