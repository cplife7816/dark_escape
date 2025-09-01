using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class KeyActivatesZoneOnMove : MonoBehaviour
{
    [Header("GameObjec")]
    [SerializeField] private GameObject[] targets;

    [Header("onActivated")]
    public UnityEvent onActivated;

    [Header("moveThreshold")]
    [SerializeField] private float moveThreshold = 0.01f;

    [Tooltip("armDelay")]
    [SerializeField] private float armDelay = 0.1f;

    private Vector3 _startPos;
    private bool _armed = false;
    private bool _triggered = false;

    private void OnEnable()
    {
        _startPos = transform.position;
        _triggered = false;
        _armed = false;
        if (armDelay <= 0f) _armed = true;
        else Invoke(nameof(Arm), armDelay);
    }

    private void Arm() => _armed = true;

    private void Update()
    {
        if (!_armed || _triggered) return;

        // ���� ��ġ���� moveThreshold �̻� ���������� �ߵ� (1ȸ��)
        if ((transform.position - _startPos).sqrMagnitude >= moveThreshold * moveThreshold)
        {
            _triggered = true;

            // 1) �� ��° ���� ������Ʈ(��) Ȱ��ȭ
            if (targets != null)
            {
                foreach (var go in targets)
                    if (go != null && !go.activeSelf) go.SetActive(true);
            }

            // 2) �̺�Ʈ ȣ�� (�ν����Ϳ��� Save �Լ� ���� ����)
            onActivated?.Invoke();
        }
    }
}
