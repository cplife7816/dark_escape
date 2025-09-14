// GlassShardCollisionHandler.cs (추가/수정)
using System.Collections;
using UnityEngine;

public class GlassShardCollisionHandler : MonoBehaviour
{
    [Tooltip("지면(센서) Y 값을 주입받아, 여기에 도달하기 전엔 비활성화하지 않음")]
    public float groundYThreshold = float.NegativeInfinity;

    [Tooltip("Y 비교 여유값(부동오차/미세 튀김 보정)")]
    public float epsilon = 0.01f;

    private bool hasLanded = false;

    // 컨트롤러/센서에서 호출
    public void SetGroundY(float y) => groundYThreshold = y;

    // 컨트롤러가 일괄 종료 전에 물어볼 때 사용할 수 있는 게이트
    public bool ReadyToDisableNow()
    {
        return transform.position.y <= groundYThreshold + epsilon || hasLanded;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasLanded) return;

        if (collision.gameObject.CompareTag("FOOTSTEPS/ROCK"))
        {
            hasLanded = true;
            StartCoroutine(DisablePhysicsAfterDelay());
        }
    }

    private IEnumerator DisablePhysicsAfterDelay()
    {
        float wait = 5f; // 기존 대기 시간 유지
        float start = Time.time;

        // 지면 Y 미도달이면 기다림(최대 wait 이후에도 미도달이면 안전 차단)
        while (Time.time - start < wait)
        {
            if (ReadyToDisableNow()) break;
            yield return null;
        }

        if (!ReadyToDisableNow())
        {
            // 대기시간이 끝났지만 아직 떠 있다면, 추가 프레임 대기(최소 한 번은 바닥에 닿을 기회 제공)
            while (!ReadyToDisableNow())
                yield return null;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        foreach (var col in GetComponents<Collider>())
            col.enabled = false;
    }
}
