using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLightController : MonoBehaviour
{
    [SerializeField] private Light playerPointLight; // 플레이어의 포인트 라이트
    [SerializeField] private float defaultIntensity = 7f; // 영향을 받지 않을 때 기본 intensity
    [SerializeField] private Color normalColor = Color.white; // 기본 색상
    [SerializeField] private Color dangerColor = Color.red;   // 낮은 intensity 시 색상
    [SerializeField] private float transitionSpeed = 2f; // 색상 및 intensity 변화 속도

    private float targetIntensity; // 목표 intensity
    private Color targetColor; // 목표 색상

    void Update()
    {
        AdjustLightIntensity(); // 목표 intensity 계산
        SmoothTransition(); // 부드러운 변화 적용
    }

    private void AdjustLightIntensity()
    {
        if (playerPointLight == null) return;

        float finalIntensity = 0f; // 가장 높은 intensity 적용
        Vector3 playerPosition = transform.position;
        bool isAffected = false; // 영향을 받고 있는지 여부 확인

        foreach (LightAffectingObject obj in LightAffectingObject.GetAllObjects())
        {
            float distance = obj.GetDistance(playerPosition);
            float maxDistance = obj.GetMaxDistance();

            if (distance < maxDistance)
            {
                isAffected = true; // 영향받는 오브젝트 존재
                // 가까울수록 intensity가 낮아지고, 멀어질수록 높아지도록 변경
                float intensity = Mathf.Lerp(obj.GetMinIntensity(), obj.GetMaxIntensity(), distance / maxDistance);
                finalIntensity = Mathf.Max(finalIntensity, intensity);
            }
        }

        // 영향을 받는 오브젝트가 없으면 기본 intensity 적용
        targetIntensity = isAffected ? finalIntensity : defaultIntensity;
        targetColor = (targetIntensity <= 5f) ? dangerColor : normalColor; // 색상 변경 로직

        // Debug 로그 추가 (값 확인용)
        // Debug.Log($"Target Intensity: {targetIntensity}, Current Intensity: {playerPointLight.intensity}");
        // Debug.Log($"Target Color: {targetColor}, Current Color: {playerPointLight.color}");
    }

    private void SmoothTransition()
    {
        if (playerPointLight == null) return;

        // 부드럽게 intensity 변경
        playerPointLight.intensity = Mathf.Lerp(playerPointLight.intensity, targetIntensity, Time.deltaTime * transitionSpeed);

        // 부드럽게 색상 변경
        playerPointLight.color = Color.Lerp(playerPointLight.color, targetColor, Time.deltaTime * transitionSpeed);
    }
}
