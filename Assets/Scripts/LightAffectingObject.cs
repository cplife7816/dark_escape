using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightAffectingObject : MonoBehaviour
{
    [SerializeField] private float maxDistance = 10f; // 이 오브젝트에 대한 최대 거리 설정
    [SerializeField] private float minIntensity = 0.5f; // 최소 조명 강도
    [SerializeField] private float maxIntensity = 5f; // 최대 조명 강도

    private static List<LightAffectingObject> allObjects = new List<LightAffectingObject>();

    private void OnEnable()
    {
        allObjects.Add(this);
    }

    private void OnDisable()
    {
        allObjects.Remove(this);
    }

    public float GetDistance(Vector3 playerPosition)
    {
        return Vector3.Distance(playerPosition, transform.position);
    }

    public float GetMaxDistance()
    {
        return maxDistance;
    }

    public float GetMinIntensity()
    {
        return minIntensity;
    }

    public float GetMaxIntensity()
    {
        return maxIntensity;
    }

    public static List<LightAffectingObject> GetAllObjects()
    {
        return allObjects;
    }
}
