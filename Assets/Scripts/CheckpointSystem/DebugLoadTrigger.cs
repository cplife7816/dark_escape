using UnityEngine;

public class DebugLoadTrigger : MonoBehaviour
{
    void Update()
    {
        // 키보드 L을 누르면 Load 실행
        if (Input.GetKeyDown(KeyCode.L))
        {
            SaveSystem.Instance.LoadCheckpoint();
            Debug.Log("체크포인트 불러오기 실행됨!");
        }
    }
}