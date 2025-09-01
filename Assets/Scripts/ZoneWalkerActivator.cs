using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// [���� �ڽ� 1����]
/// - Player�� Ʈ���ſ� �����ϸ�, ������ Walker���� Ȱ��ȭ�ϰ�
///   �� Ʈ���� ������Ʈ �ڽ��� ��Ȱ��ȭ�Ѵ�.
/// - Walker Ȱ��ȭ ���:
///   1) walkerRootsToActivate: GameObject Ȱ��ȭ(SetActive(true))
///   2) walkerBehavioursToEnable: ������Ʈ.enabled = true
/// </summary>
[RequireComponent(typeof(Collider))]
public class ZoneWalkerActivator : MonoBehaviour
{
    [Header("UsePlayerTag")]
    public bool usePlayerTag = true;

    public string playerTag = "Player";

    [Header("WalkerRootsToActivate")]
    public List<GameObject> walkerRootsToActivate = new List<GameObject>();

    [Header("Option")]
    public bool deactivateThisObjectAfterTrigger = true;

    [Tooltip("SelfDeactivateDelay")]
    public float selfDeactivateDelay = 0f;

    [Header("Event")]
    [Tooltip("Walker Ȱ��ȭ ���� ȣ���� �̺�Ʈ(����/����Ʈ �� �� ����)")]
    public UnityEvent onActivated;

    private bool _hasTriggered = false;

    private void Reset()
    {
        // �ݶ��̴��� Trigger�� ����
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        // ���� ���� ��� �� MeshRenderer�� �ִٸ� ���� �� ����
        var mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasTriggered) return;
        if (!IsPlayer(other)) return;

        _hasTriggered = true;
        ActivateWalkers();
        onActivated?.Invoke();

        if (deactivateThisObjectAfterTrigger)
        {
            if (selfDeactivateDelay <= 0f) gameObject.SetActive(false);
            else Invoke(nameof(DeactivateSelf), selfDeactivateDelay);
        }
    }

    private bool IsPlayer(Collider other)
    {
        if (usePlayerTag) return other.CompareTag(playerTag);

        // �±� ��� �÷��̾� ��Ʈ�ѷ� ����� ���� (������Ʈ�� ���� Ŭ������ �ٲ㵵 OK)
        return other.GetComponentInParent<FirstPersonController>() != null;
    }

    private void ActivateWalkers()
    {
        // 1) ��Ʈ ������Ʈ Ȱ��ȭ
        foreach (var go in walkerRootsToActivate)
        {
            if (go == null) continue;
            if (!go.activeSelf) go.SetActive(true);
        }

        Debug.Log("[ZoneWalkerActivator] ������ Walker���� Ȱ��ȭ�߽��ϴ�.");
    }

    private void DeactivateSelf()
    {
        gameObject.SetActive(false);
    }
}
