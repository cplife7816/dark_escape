using UnityEngine;

public class MonsterLightAlert : MonoBehaviour
{
    [SerializeField] private Light monsterPointLight1; // ���Ϳ� ����� ����Ʈ ����Ʈ
    [SerializeField] private Light monsterPointLight2; // ���Ϳ� ����� ����Ʈ ����Ʈ
    [SerializeField] private Color alertColor = Color.red; // ���� ��� ����
    [SerializeField] private Color warningColor = Color.magenta; // �߰� ��� ����
    [SerializeField] private Color defaultColor = Color.white; // �⺻ ����Ʈ ����
    [SerializeField] private float warningThreshold = 3f; // ��ȫ������ ���ϱ� ���� �ð� (��)
    [SerializeField] private float alertThreshold = 6f; // ���������� ���ϱ� ���� �ð� (��)
    [SerializeField] private float resetThreshold = 3f; // ���� �ۿ� �־�� �ϴ� �ð� (��)

    private Transform playerCamera;
    private FirstPersonController playerController;

    private float timeInRange = 0f;
    private float timeOutOfRange = 0f;
    private enum AlertState { Default, Warning, Alert }
    private AlertState currentState = AlertState.Default;

    void Start()
    {
        playerController = FindObjectOfType<FirstPersonController>(); // FirstPersonController ��������
        if (playerController != null)
        {
            playerCamera = playerController.GetComponentInChildren<Camera>().transform; // �÷��̾� ī�޶� ��������
        }
    }

    void Update()
    {
        if (playerController != null && playerCamera != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerCamera.position);
            float playerLightRange = playerController.GetPointLightRange(); // FirstPersonController���� ����Ʈ ���� ��������

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
