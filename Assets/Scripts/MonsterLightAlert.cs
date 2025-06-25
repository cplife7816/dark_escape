using UnityEngine;

public class MonsterLightAlert : MonoBehaviour
{
    [SerializeField] private Light monsterPointLight1; // 몬스터에 적용된 포인트 라이트
    [SerializeField] private Light monsterPointLight2; // 몬스터에 적용된 포인트 라이트
    [SerializeField] private Color alertColor = Color.red; // 최종 경고 색상
    [SerializeField] private Color warningColor = Color.magenta; // 중간 경고 색상
    [SerializeField] private Color defaultColor = Color.white; // 기본 라이트 색상
    [SerializeField] private float warningThreshold = 3f; // 분홍색으로 변하기 위한 시간 (초)
    [SerializeField] private float alertThreshold = 6f; // 빨간색으로 변하기 위한 시간 (초)
    [SerializeField] private float resetThreshold = 3f; // 범위 밖에 있어야 하는 시간 (초)

    private Transform playerCamera;
    private FirstPersonController playerController;

    private float timeInRange = 0f;
    private float timeOutOfRange = 0f;
    private enum AlertState { Default, Warning, Alert }
    private AlertState currentState = AlertState.Default;

    void Start()
    {
        playerController = FindObjectOfType<FirstPersonController>(); // FirstPersonController 가져오기
        if (playerController != null)
        {
            playerCamera = playerController.GetComponentInChildren<Camera>().transform; // 플레이어 카메라 가져오기
        }
    }

    void Update()
    {
        if (playerController != null && playerCamera != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerCamera.position);
            float playerLightRange = playerController.GetPointLightRange(); // FirstPersonController에서 라이트 범위 가져오기

            if (playerLightRange > distanceToPlayer)
            {
                timeInRange += Time.deltaTime;
                timeOutOfRange = 0f;

                if (timeInRange >= alertThreshold && currentState != AlertState.Alert)
                {
                    ChangeLightColor(alertColor);
                    currentState = AlertState.Alert;
                }
                else if (timeInRange >= warningThreshold && currentState == AlertState.Default)
                {
                    ChangeLightColor(warningColor);
                    currentState = AlertState.Warning;
                }
            }
            else
            {
                timeOutOfRange += Time.deltaTime;
                timeInRange = 0f;

                if (timeOutOfRange >= resetThreshold && currentState != AlertState.Default)
                {
                    ChangeLightColor(defaultColor);
                    currentState = AlertState.Default;
                }
            }
        }
    }

    private void ChangeLightColor(Color color)
    {
        monsterPointLight1.color = color;
        monsterPointLight2.color = color;
    }
}
