using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// [투명 박스 2번용]
/// - Player가 트리거에 진입하면, 지정한 Walker들을 비활성화하고
///   문을 닫고 잠근다.
/// - 문 제어 우선순위:
///   1) doorObject에 대해 SendMessage("Close") / SendMessage("Lock") 시도
///   2) doorObject의 컴포넌트에서 IsLocked/locked 같은 bool 필드 true로 설정 시도
///   3) (필요 시) onDoorCloseAndLock 이벤트로 직접 메서드 연결
/// </summary>
[RequireComponent(typeof(Collider))]
public class ZoneWalkerDeactivatorAndDoorLocker : MonoBehaviour
{
    [Header("플레이어 판정")]
    public bool usePlayerTag = true;
    public string playerTag = "Player";

    [Header("비활성화 대상 (둘 중 필요한 것만 채우세요)")]
    [Tooltip("비활성화할 Walker 루트 게임오브젝트들 (SetActive(false))")]
    public List<GameObject> walkerRootsToDeactivate = new List<GameObject>();

    [Tooltip("비활성화할 Walker 관련 컴포넌트들 (enabled = false)")]
    public List<Behaviour> walkerBehavioursToDisable = new List<Behaviour>();

    [Header("문 제어")]
    [Tooltip("닫고 잠글 문 오브젝트(문 루트 또는 Door 스크립트가 붙은 오브젝트)")]
    public GameObject doorObject;

    [Tooltip("문 닫기 메시지 이름 (프로젝트 문 스크립트 메서드명에 맞춰 변경 가능)")]
    public string closeMethodName = "Close";

    [Tooltip("문 잠금 메시지 이름 (프로젝트 문 스크립트 메서드명에 맞춰 변경 가능)")]
    public string lockMethodName = "Lock";

    [Tooltip("잠금 상태로 추정되는 필드 후보들(순서대로 탐색)")]
    public List<string> lockFieldCandidates = new List<string> { "IsLocked", "isLocked", "Locked", "locked" };

    [Header("이벤트(선택)")]
    [Tooltip("Walker 비활성화 직후 호출(사운드/라이트 등)")]
    public UnityEvent onDeactivated;

    [Tooltip("문 닫기/잠금 이후에 추가로 호출할 이벤트(애니메이션 트리거 등 직접 연결)")]
    public UnityEvent onDoorCloseAndLock;

    [Header("트리거 1회성 처리")]
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

        Debug.Log("[ZoneWalkerDeactivatorAndDoorLocker] 지정한 Walker들을 비활성화했습니다.");
    }

    private void CloseAndLockDoor()
    {
        if (doorObject == null)
        {
            Debug.LogWarning("[ZoneWalkerDeactivatorAndDoorLocker] doorObject가 지정되지 않았습니다.");
            return;
        }

        // 1) 메서드 호출 시도 (없어도 오류 없음)
        //    - SendMessage는 하위 컴포넌트까지 브로드캐스트하지 않으므로,
        //      Door 루트에 붙어있다고 가정하거나, 필요한 경우 doorObject를 Door 컴포넌트가 붙은 오브젝트로 직접 지정하세요.
        doorObject.SendMessage(closeMethodName, SendMessageOptions.DontRequireReceiver);
        doorObject.SendMessage(lockMethodName, SendMessageOptions.DontRequireReceiver);

        // 2) bool 잠금 필드 설정 시도 (리플렉션)
        TrySetLockFieldTrue(doorObject);

        Debug.Log("[ZoneWalkerDeactivatorAndDoorLocker] 문 닫기/잠금 동작을 시도했습니다.");
    }

    private void TrySetLockFieldTrue(GameObject target)
    {
        var components = target.GetComponents<Component>();
        foreach (var comp in components)
        {
            if (comp == null) continue;
            var type = comp.GetType();
            // public / non-public 모두 탐색
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            foreach (var fieldName in lockFieldCandidates)
            {
                var f = type.GetField(fieldName, flags);
                if (f != null && f.FieldType == typeof(bool))
                {
                    try
                    {
                        f.SetValue(comp, true);
                        Debug.Log($"[ZoneWalkerDeactivatorAndDoorLocker] {type.Name}.{fieldName} = true 로 설정");
                        return;
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[ZoneWalkerDeactivatorAndDoorLocker] {type.Name}.{fieldName} 설정 실패: {e.Message}");
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
