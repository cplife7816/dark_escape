using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLightController : MonoBehaviour
{
    [SerializeField] private Light playerPointLight; // �÷��̾��� ����Ʈ ����Ʈ
    [SerializeField] private float defaultIntensity = 7f; // ������ ���� ���� �� �⺻ intensity
    [SerializeField] private Color normalColor = Color.white; // �⺻ ����
    [SerializeField] private Color dangerColor = Color.red;   // ���� intensity �� ����
    [SerializeField] private float transitionSpeed = 2f; // ���� �� intensity ��ȭ �ӵ�

    private float targetIntensity; // ��ǥ intensity
    private Color targetColor; // ��ǥ ����

    void Update()
    {
        AdjustLightIntensity(); // ��ǥ intensity ���
        SmoothTransition(); // �ε巯�� ��ȭ ����
    }

    private void AdjustLightIntensity()
    {
        if (playerPointLight == null) return;

        float finalIntensity = 0f; // ���� ���� intensity ����
        Vector3 playerPosition = transform.position;
        bool isAffected = false; // ������ �ް� �ִ��� ���� Ȯ��

        foreach (LightAffectingObject obj in LightAffectingObject.GetAllObjects())
        {
            float distance = obj.GetDistance(playerPosition);
            float maxDistance = obj.GetMaxDistance();

            if (distance < maxDistance)
            {
                isAffected = true; // ����޴� ������Ʈ ����
                // �������� intensity�� ��������, �־������� ���������� ����
                float intensity = Mathf.Lerp(obj.GetMinIntensity(), obj.GetMaxIntensity(), distance / maxDistance);
                finalIntensity = Mathf.Max(finalIntensity, intensity);
            }
        }

        // ������ �޴� ������Ʈ�� ������ �⺻ intensity ����
        targetIntensity = isAffected ? finalIntensity : defaultIntensity;
        targetColor = (targetIntensity <= 5f) ? dangerColor : normalColor; // ���� ���� ����

        // Debug �α� �߰� (�� Ȯ�ο�)
        // Debug.Log($"Target Intensity: {targetIntensity}, Current Intensity: {playerPointLight.intensity}");
        // Debug.Log($"Target Color: {targetColor}, Current Color: {playerPointLight.color}");
    }

    private void SmoothTransition()
    {
        if (playerPointLight == null) return;

        // �ε巴�� intensity ����
        playerPointLight.intensity = Mathf.Lerp(playerPointLight.intensity, targetIntensity, Time.deltaTime * transitionSpeed);

        // �ε巴�� ���� ����
        playerPointLight.color = Color.Lerp(playerPointLight.color, targetColor, Time.deltaTime * transitionSpeed);
    }
}
