// IGameOverFinisher.cs
using System.Collections;

public interface IGameOverFinisher
{
    // 플레이어가 이 코루틴을 실행하면, 각 적 전용 연출이 재생된다.
    IEnumerator Play(FirstPersonController player);

    // (선택) 분석/로그용 식별자
    string ReasonId { get; }
}
