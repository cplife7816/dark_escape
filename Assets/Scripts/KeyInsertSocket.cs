using UnityEngine;

public class KeyInsertSocket : MonoBehaviour, IItemSocket
{
    [Header("Required Key Name")]
    [SerializeField] private string requiredKeyName;  // 이름 비교 기준

    [Header("Object to Activate")]
    [SerializeField] private GameObject keyVisualObject;  // 열쇠 비주얼 오브젝트

    [Header("Sound Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip insertSound;

    private bool isUsed = false;

    public bool TryInteract(GameObject item)
    {
        if (isUsed || item == null || item.name != requiredKeyName)
            return false;

        Debug.Log($"[KeyInsertSocket] 올바른 열쇠({item.name}) 감지됨 → 삽입 처리");

        isUsed = true;

        // 🔊 효과음 재생
        if (audioSource != null && insertSound != null)
            audioSource.PlayOneShot(insertSound);

        // 🧱 비주얼 오브젝트 활성화 (ex. 꽂힌 열쇠)
        if (keyVisualObject != null)
            keyVisualObject.SetActive(true);

        // 🔒 플레이어 손의 오브젝트 비활성화
        item.SetActive(false);

        return true;  // 드롭 처리되도록 true 반환
    }

    public bool CanInteract(GameObject item)
    {
        return !isUsed && item != null && item.name == requiredKeyName;
    }
}
