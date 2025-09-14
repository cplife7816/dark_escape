using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightAffectingObject : MonoBehaviour
{
    [SerializeField] private float maxDistance = 10f; // �� ������Ʈ�� ���� �ִ� �Ÿ� ����
    [SerializeField] private float minIntensity = 0.5f; // �ּ� ���� ����
    [SerializeField] private float maxIntensity = 5f; // �ִ� ���� ����

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
