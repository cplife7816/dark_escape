using UnityEngine;

public class DebugLoadTrigger : MonoBehaviour
{
    void Update()
    {
        // Ű���� L�� ������ Load ����
        if (Input.GetKeyDown(KeyCode.L))
        {
            SaveSystem.Instance.LoadCheckpoint();
            Debug.Log("üũ����Ʈ �ҷ����� �����!");
        }
    }
}