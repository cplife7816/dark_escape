using UnityEngine;

/// <summary>
/// ī��Ű�� '�θ�'�� �ٲ�� ������ �����Ͽ�,
/// �� �θ� ü�ο� FirstPersonController�� ������ = �÷��̾ ���� ������ ����.
/// �� ��� MimicCommandOnInteract�� �˷� �ֺ� Walker���� Rage+�߰��� �����ϰ� �Ѵ�.
/// </summary>
public class KeycardPickupWatcher : MonoBehaviour
{
    [Header("Mimic Link")]
    [Tooltip("�� ī��Ű�� ����(�θ�)�ϴ� Mimic�� Ŀ�ǵ� ������Ʈ. ����θ� �������� �ڵ� Ž��.")]
    [SerializeField] private MimicCommandOnInteract mimic;

    [Header("Trigger Options")]
    [Tooltip("�� �θ� ü�ο� FirstPersonController�� �־�߸� Ʈ�������� ����")]
    [SerializeField] private bool requirePlayerParent = true;

    [Tooltip("�� ���� Ʈ����(�ߺ� ����)")]
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered;

    private void Awake()
    {
        if (mimic == null)
            mimic = GetComponentInParent<MimicCommandOnInteract>();
        if (mimic == null)
            Debug.LogWarning("[KeycardPickupWatcher] MimicCommandOnInteract�� �θ� ü�ο��� ã�� ���߽��ϴ�.");
    }

    // �θ� �ٲ�� ��� ȣ���(�÷��̾� ���� Hold Ʈ���������� �Űܰ� �� ��)
    private void OnTransformParentChanged()
    {
        TryTriggerByNewParent(transform.parent);
    }

    // ī��Ű�� ��Ȱ��/�ı��Ǹ� �θ� ����� ��쵵 ��ȣ�� ���(�ɼ�)
    private void OnDisable()
    {
        // �ʿ� ���ٸ� �ּ� ó�� ����
        TryTriggerByNewParent(transform.parent);
    }

    private void TryTriggerByNewParent(Transform newParent)
    {
        if (mimic == null) return;
        if (triggerOnce && hasTriggered) return;

        // ���� Mimic�� �ڽ�(Ȥ�� ����) ���¶�� '���Ⱦ�'���� ����
        if (mimic.transform != null && transform.IsChildOf(mimic.transform))
            return;

        // �÷��̾ �θ� ü�ο� �ִ��� Ȯ��
        FirstPersonController player = null;
        if (requirePlayerParent && newParent != null)
        {
            player = newParent.GetComponentInParent<FirstPersonController>();
            if (player == null) return; // �÷��̾� ���� �ƴϸ� ����
        }
        else
        {
            // ������ġ: ������ �÷��̾� Ž��
            player = FindObjectOfType<FirstPersonController>();
        }

        // Mimic���� ���÷��̾ ��ȣ�ۿ��ߴ١� �˸��� �� Mimic�� �ֺ� Walker���� Rage+�߰� ����
        mimic.InteractByPlayer(player);

        if (triggerOnce) hasTriggered = true;
    }
}
