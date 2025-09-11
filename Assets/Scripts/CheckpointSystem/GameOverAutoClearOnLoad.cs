// GameOverAutoClearOnLoad.cs
using UnityEngine;
using UnityEngine.UI;

public class GameOverAutoClearOnLoad : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private FirstPersonController fpc;

    [Tooltip("게임오버에 쓰였던 오버레이/화면 Image, CanvasGroup, GameObject 등을 넣어두세요")]
    [SerializeField] private GameObject[] gameOverObjects;
    [SerializeField] private CanvasGroup[] gameOverCanvasGroups;
    [SerializeField] private Image[] gameOverImages;

    [Header("Options")]
    [SerializeField] private bool resetTimeScale = true;
    [SerializeField] private bool lockCursor = true;

    void OnEnable() => SaveSystem.AfterLoad += HandleAfterLoad;
    void OnDisable() => SaveSystem.AfterLoad -= HandleAfterLoad;

    private void HandleAfterLoad()
    {
        // 1) 게임오버 UI/오브젝트 숨김
        if (gameOverObjects != null)
            foreach (var go in gameOverObjects) if (go) go.SetActive(false);

        if (gameOverCanvasGroups != null)
            foreach (var cg in gameOverCanvasGroups)
                if (cg) { cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false; }

        if (gameOverImages != null)
            foreach (var img in gameOverImages) if (img) img.enabled = false;

        // 2) 플레이어 조작 복구
        if (fpc)
        {
            // 프로젝트에 맞게 조작 플래그/상태를 복구하세요.
            // 예시: fpc.canMove = true; fpc.EnableInput(true); 등
            TryEnableMovement(fpc, true);
        }

        // 3) 타임/커서 복구
        if (resetTimeScale) Time.timeScale = 1f;
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (fpc)
        {
            fpc.SetCanMove(true);   // ⬅️ 여기서 다시 활성화
        }
    }

    // FirstPersonController에 맞는 메서드가 없다면 간단 래퍼를 사용
    private void TryEnableMovement(FirstPersonController controller, bool enable)
    {
        // 컨트롤러에 따라 다를 수 있으니 안전하게 처리
        // 예: controller.SetCanMove(enable); controller.enabled = true; 등
        controller.enabled = true;
        // 필요 시, 다음과 같이 프로젝트 메서드를 호출:
        // controller.ForceRelease();  // 손에 든 아이템 해제하고 싶을 때
        // controller.ResetHoldPosition();
        // controller.SetInputEnabled(enable); // 이런 메서드가 있다면 호출
    }
}
