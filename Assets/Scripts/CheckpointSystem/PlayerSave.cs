using UnityEngine;
using System.Collections;

public class PlayerSave : MonoBehaviour, ISaveable
{
    [Header("Refs")]
    [SerializeField] private Transform playerRoot;          // FPC 루트 트랜스폼
    [SerializeField] private FirstPersonController fpc;     // FPC 스크립트
    [SerializeField] private Transform headOrCamera;        // 카메라/헤드(피치가 적용되는 Transform)

    [Header("Options")]
    [SerializeField] private bool useCharacterControllerFix = true;

    [System.Serializable]
    struct Data
    {
        public Vector3 pos;
        public Quaternion rot;        // 플레이어 루트 회전(보통 요)
        public Quaternion headLocal;  // 카메라/헤드 로컬 회전(보통 피치)
        public string heldItemId;     // 손에 든 아이템 SaveableEntity.Id
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

        // 1) CC/물리 보정 끄고 정확좌표로 텔레포트
        if (cc) cc.enabled = false;
        root.SetPositionAndRotation(d.pos, d.rot);
        if (headOrCamera) headOrCamera.localRotation = d.headLocal;

        // 2) 한 프레임 양보(다른 세이브들이 먼저 복원되게)
        yield return null;

        // 3) 들고 있던 아이템 재장착
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

        // 4) CC 재활성 → 이후 중력/지면 스냅 정상 동작
        if (cc) cc.enabled = true;
    }
}
