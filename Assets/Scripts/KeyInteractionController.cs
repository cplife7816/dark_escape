using UnityEngine;

[RequireComponent(typeof(Door))]
public class KeyInteractionController : MonoBehaviour, IItemSocket
{
    [Header("Key_Name")]
    [SerializeField] private string requiredKeyName;

    [Header("Paired Door")]
    [SerializeField] private Door pairedDoor; // Null 가능

    [Header("Unlock Sound")]
    [SerializeField] private AudioClip unlockSound; // 🔊 지정 가능

    private Door thisDoor;

    private void Awake()
    {
        thisDoor = GetComponent<Door>();
        if (thisDoor == null)
            Debug.LogError("[KeyInteraction] Door 컴포넌트를 찾지 못했습니다.");
    }

    public bool TryInteract(GameObject item)
    {
        if (item == null || string.IsNullOrEmpty(requiredKeyName))
            return false;

        if (item.name != requiredKeyName)
            return false;

        Debug.Log($"[KeyInteraction] 열쇠({item.name})로 {thisDoor.name} 잠금 해제");

        // 1. 자신 해제 및 사운드
        thisDoor.isLocked = false;
        TryPlaySound(thisDoor);

        // 2. 반대 문도 해제 및 사운드
        if (pairedDoor != null)
        {
            pairedDoor.isLocked = false;
            TryPlaySound(pairedDoor);
        }

        // 3. 열쇠 비활성화
        item.SetActive(false);
        return true;
    }

    private void TryPlaySound(Door door)
    {
        if (door.TryGetComponent(out AudioSource audioSource) && unlockSound != null)
        {
            audioSource.PlayOneShot(unlockSound);
        }
    }

    public bool CanInteract(GameObject item)
    {
        return item != null && item.name == requiredKeyName;
    }

}
