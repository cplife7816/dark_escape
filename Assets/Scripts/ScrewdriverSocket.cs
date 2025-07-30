using UnityEngine;

public class ScrewdriverSocket : MonoBehaviour, IItemSocket
{
    [Header("driver")]
    [SerializeField] private ScrewDriverController controller;

    private bool isCombined = false;

    public bool TryInteract(GameObject item)
    {
        if (isCombined || item == null)
            return false;

        // 조건: 이름 비교로 Driver_Handle일 경우만 결합 허용
        if (!item.name.Contains("Driver_Handle"))
        {
            Debug.LogWarning("[ScrewdriverSocket] Driver_Handle이 아닌 오브젝트와 상호작용 시도");
            return false;
        }

        // ⬇ 현재 Screw (이 스크립트가 붙은 오브젝트)의 위치/회전 저장
        Vector3 spawnPosition = transform.position;
        Quaternion spawnRotation = transform.rotation;

        // ✅ 완성 드라이버 활성화 호출
        if (controller != null)
        {
            controller.PlaySoundAndActivateComplete(spawnPosition, spawnRotation);
        }
        else
        {
            Debug.LogError("[ScrewdriverSocket] controller 연결 누락됨!");
        }

        // ✅ Driver_Handle과 Screw 모두 비활성화
        item.SetActive(false);          // Driver_Handle 비활성화
        gameObject.SetActive(false);    // Screw 비활성화 (이 스크립트가 붙은 오브젝트)

        isCombined = true;
        return true;
    }

    public bool CanInteract(GameObject item)
    {
        return !isCombined && item != null && item.name.Contains("Driver_Handle");
    }

}
