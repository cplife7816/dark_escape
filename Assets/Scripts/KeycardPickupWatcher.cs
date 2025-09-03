using UnityEngine;

/// <summary>
/// 카드키의 '부모'가 바뀌는 순간을 감지하여,
/// 새 부모 체인에 FirstPersonController가 있으면 = 플레이어가 집은 것으로 간주.
/// 그 즉시 MimicCommandOnInteract로 알려 주변 Walker에게 Rage+추격을 지시하게 한다.
/// </summary>
public class KeycardPickupWatcher : MonoBehaviour
{
    [Header("Mimic Link")]
    [Tooltip("이 카드키를 소유(부모)하던 Mimic의 커맨드 컴포넌트. 비워두면 상위에서 자동 탐색.")]
    [SerializeField] private MimicCommandOnInteract mimic;

    [Header("Trigger Options")]
    [Tooltip("새 부모 체인에 FirstPersonController가 있어야만 트리거할지 여부")]
    [SerializeField] private bool requirePlayerParent = true;

    [Tooltip("한 번만 트리거(중복 방지)")]
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered;

    private void Awake()
    {
        if (mimic == null)
            mimic = GetComponentInParent<MimicCommandOnInteract>();
        if (mimic == null)
            Debug.LogWarning("[KeycardPickupWatcher] MimicCommandOnInteract를 부모 체인에서 찾지 못했습니다.");
    }

    // 부모가 바뀌는 즉시 호출됨(플레이어 손의 Hold 트랜스폼으로 옮겨갈 때 등)
    private void OnTransformParentChanged()
    {
        TryTriggerByNewParent(transform.parent);
    }

    // 카드키가 비활성/파괴되며 부모가 끊기는 경우도 신호로 취급(옵션)
    private void OnDisable()
    {
        // 필요 없다면 주석 처리 가능
        TryTriggerByNewParent(transform.parent);
    }

    private void TryTriggerByNewParent(Transform newParent)
    {
        if (mimic == null) return;
        if (triggerOnce && hasTriggered) return;

        // 아직 Mimic의 자식(혹은 하위) 상태라면 '미픽업'으로 간주
        if (mimic.transform != null && transform.IsChildOf(mimic.transform))
            return;

        // 플레이어가 부모 체인에 있는지 확인
        FirstPersonController player = null;
        if (requirePlayerParent && newParent != null)
        {
            player = newParent.GetComponentInParent<FirstPersonController>();
            if (player == null) return; // 플레이어 손이 아니면 보류
        }
        else
        {
            // 안전장치: 씬에서 플레이어 탐색
            player = FindObjectOfType<FirstPersonController>();
        }

        // Mimic에게 “플레이어가 상호작용했다” 알리기 → Mimic이 주변 Walker에게 Rage+추격 지시
        mimic.InteractByPlayer(player);

        if (triggerOnce) hasTriggered = true;
    }
}
