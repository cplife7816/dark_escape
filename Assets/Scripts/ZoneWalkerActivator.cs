using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// [투명 박스 1번용]
/// - Player가 트리거에 진입하면, 지정한 Walker들을 활성화하고
///   이 트리거 오브젝트 자신은 비활성화한다.
/// - Walker 활성화 방식:
///   1) walkerRootsToActivate: GameObject 활성화(SetActive(true))
///   2) walkerBehavioursToEnable: 컴포넌트.enabled = true
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
    [Tooltip("Walker 활성화 직후 호출할 이벤트(사운드/라이트 등 훅 연결)")]
    public UnityEvent onActivated;

    private bool _hasTriggered = false;

    private void Reset()
    {
        // 콜라이더를 Trigger로 강제
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        // 투명 상자 사용 시 MeshRenderer가 있다면 끄는 걸 권장
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

        // 태그 대신 플레이어 컨트롤러 존재로 판정 (프로젝트에 맞춰 클래스명 바꿔도 OK)
        return other.GetComponentInParent<FirstPersonController>() != null;
    }

    private void ActivateWalkers()
    {
        // 1) 루트 오브젝트 활성화
        foreach (var go in walkerRootsToActivate)
        {
            if (go == null) continue;
            if (!go.activeSelf) go.SetActive(true);
        }

        Debug.Log("[ZoneWalkerActivator] 지정한 Walker들을 활성화했습니다.");
    }

    private void DeactivateSelf()
    {
        gameObject.SetActive(false);
    }
}
