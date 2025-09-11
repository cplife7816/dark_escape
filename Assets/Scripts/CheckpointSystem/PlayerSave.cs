using UnityEngine;
using System.Collections;

public class PlayerSave : MonoBehaviour, ISaveable
{
    [Header("Refs")]
    [SerializeField] private Transform playerRoot;          // FPC ��Ʈ Ʈ������
    [SerializeField] private FirstPersonController fpc;     // FPC ��ũ��Ʈ
    [SerializeField] private Transform headOrCamera;        // ī�޶�/���(��ġ�� ����Ǵ� Transform)

    [Header("Options")]
    [SerializeField] private bool useCharacterControllerFix = true;

    [System.Serializable]
    struct Data
    {
        public Vector3 pos;
        public Quaternion rot;        // �÷��̾� ��Ʈ ȸ��(���� ��)
        public Quaternion headLocal;  // ī�޶�/��� ���� ȸ��(���� ��ġ)
        public string heldItemId;     // �տ� �� ������ SaveableEntity.Id
    }

    public string CaptureState()
    {
        var root = playerRoot ? playerRoot : transform;
        var d = new Data
        {
            pos = root.position,
            rot = root.rotation,
            headLocal = headOrCamera ? headOrCamera.localRotation : Quaternion.identity,
            heldItemId = (fpc != null && fpc.HeldObject != null)
                ? fpc.HeldObject.GetComponent<SaveableEntity>()?.Id
                : null
        };
        return JsonUtility.ToJson(d);
    }

    public void RestoreState(string json)
    {
        var d = JsonUtility.FromJson<Data>(json);
        StartCoroutine(RestoreRoutine(d));
    }

    private IEnumerator RestoreRoutine(Data d)
    {
        var root = playerRoot ? playerRoot : transform;
        var cc = useCharacterControllerFix ? GetComponent<CharacterController>() : null;

        // 1) CC/���� ���� ���� ��Ȯ��ǥ�� �ڷ���Ʈ
        if (cc) cc.enabled = false;
        root.SetPositionAndRotation(d.pos, d.rot);
        if (headOrCamera) headOrCamera.localRotation = d.headLocal;

        // 2) �� ������ �纸(�ٸ� ���̺���� ���� �����ǰ�)
        yield return null;

        // 3) ��� �ִ� ������ ������
        if (fpc)
        {
            if (!string.IsNullOrEmpty(d.heldItemId))
            {
                var all = Resources.FindObjectsOfTypeAll<SaveableEntity>();
                var target = System.Array.Find(all, a => a.Id == d.heldItemId);
                if (target != null)
                {
                    if (!target.gameObject.activeInHierarchy) target.gameObject.SetActive(true);
                    fpc.ForceHold(target.gameObject);
                }
                else fpc.ForceRelease();
            }
            else fpc.ForceRelease();
        }

        // 4) CC ��Ȱ�� �� ���� �߷�/���� ���� ���� ����
        if (cc) cc.enabled = true;
    }
}
