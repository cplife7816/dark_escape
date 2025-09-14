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

        // 시작 위치에서 moveThreshold 이상 움직였으면 발동 (1회성)
        if ((transform.position - _startPos).sqrMagnitude >= moveThreshold * moveThreshold)
        {
            _triggered = true;

            // 1) 두 번째 투명 오브젝트(들) 활성화
            if (targets != null)
            {
                foreach (var go in targets)
                    if (go != null && !go.activeSelf) go.SetActive(true);
            }

            // 2) 이벤트 호출 (인스펙터에서 Save 함수 연결 예정)
            onActivated?.Invoke();
        }
    }
}
