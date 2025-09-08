// KeyUnlockHingedBox.cs
using UnityEngine;

/// HingedBox를 열쇠로 잠금해제.
/// - IItemSocket의 CanInteract / TryInteract 둘 다 구현(프로젝트별 인터페이스 차이 대응)
/// - KeyInteractionController(Door 전용)과 동일하게: 성공 시 true 반환 → FPC가 Drop 처리
public class KeyUnlockHingedBox : MonoBehaviour, IItemSocket
{
    [Header("Key Name")]
    [SerializeField] private string requiredKeyName;

    [Header("Target Box (optional)")]
    [SerializeField] private HingedBoxInteraction targetBox; // 비우면 자기 자신에서 찾음

    [Header("Sound (optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip unlockSound;

    [Header("Behavior")]
    [SerializeField] private bool consumeKeyOnUnlock = true; // 해제 시 열쇠 비활성화
    [SerializeField] private bool onlyWhenLocked = true;     // 잠겨있을 때만 동작

    private void Awake()
    {
        if (!targetBox) targetBox = GetComponent<HingedBoxInteraction>();
        if (!audioSource) audioSource = GetComponent<AudioSource>();
    }

    // ▸ 어떤 키로 상호작용 가능한지(선검사). 프로젝트에 따라 이 메서드가 인터페이스에 요구될 수 있음.
    public bool CanInteract(GameObject item)
    {
        if (item == null) return false;
        if (string.IsNullOrEmpty(requiredKeyName)) return false;
        if (!targetBox) return false;
        if (onlyWhenLocked && !targetBox.isLocked) return false;
        return item.name == requiredKeyName;
    }

    // ▸ 실제 상호작용(해제 시도). TryInteract만 요구하는 프로젝트도 있으므로 함께 구현.
    public bool TryInteract(GameObject item)
    {
        if (!CanInteract(item)) return false;

        // 잠금 해제
        targetBox.isLocked = false;

        // 사운드
        if (audioSource && unlockSound) audioSource.PlayOneShot(unlockSound);

        // 열쇠 소비 (KeyInteractionController와 동일한 정책)
        if (consumeKeyOnUnlock && item.activeSelf)
            item.SetActive(false);

        // true 반환 → FPC가 DropItem() 처리 (네 현재 FPC 로직과 호환)
        return true;
    }
}
