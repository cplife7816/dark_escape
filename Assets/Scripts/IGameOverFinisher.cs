// IGameOverFinisher.cs
using System.Collections;

public interface IGameOverFinisher
{
    // �÷��̾ �� �ڷ�ƾ�� �����ϸ�, �� �� ���� ������ ����ȴ�.
    IEnumerator Play(FirstPersonController player);

    // (����) �м�/�α׿� �ĺ���
    string ReasonId { get; }
}
