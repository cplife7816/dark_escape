using UnityEngine;

public class GownInteraction : MonoBehaviour, ITryInteractable
{
    [SerializeField] private GameObject longGown;   // 긴 가운
    [SerializeField] private GameObject shortGown;  // 짧은 가운

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip gownSound;

    private bool hasSwitched = false;
    private BoxCollider boxCollider;

    private void Awake()
    {
        // 같은 오브젝트에 붙은 BoxCollider 참조
        boxCollider = GetComponent<BoxCollider>();
    }

    private void Start()
    {
        // 초기 상태: 긴 가운 켜고 짧은 가운 끄기
        if (longGown != null) longGown.SetActive(true);
        if (shortGown != null) shortGown.SetActive(false);
    }

    public void TryInteract()
    {
        if (hasSwitched) return; // 단 한번만 전환

        if (longGown != null) longGown.SetActive(false);
        if (shortGown != null) shortGown.SetActive(true);

        if (audioSource && gownSound)
            audioSource.PlayOneShot(gownSound);

        // ✅ 더 이상 상호작용 필요 없으니 BoxCollider 비활성화
        if (boxCollider != null)
            boxCollider.enabled = false;

        hasSwitched = true;
        Debug.Log("[GownInteraction] 긴 가운 → 짧은 가운 전환됨, 상호작용 종료");
    }
}
