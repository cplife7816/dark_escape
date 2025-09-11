using UnityEngine;

[DisallowMultipleComponent]
public class GlassBottleSave : MonoBehaviour, ISaveable
{
    [Header("Hierarchy Refs")]
    [Tooltip("온전한 메쉬(파손 전) 루트")]
    [SerializeField] private GameObject intactRoot;          // 예: 실제 메쉬, pointLight 포함
    [Tooltip("파편들이 담긴 부모 Transform")]
    [SerializeField] private Transform fragmentsParent;       // 예: "fragments"
    [Tooltip("Red glass 박스콜라이더(충격/깨짐 판정용)")]
    [SerializeField] private Collider redGlassCollider;       // 예: red_glass(옵션)

    [Header("Optional")]
    [SerializeField] private Light pointLight;                // 파손시 라이트 효과를 끄거나 초기화
    [SerializeField] private bool savePhysicsForFragments = false; // 풀 스냅샷 켜기

    // 외부 메인 스크립트가 isShattered를 관리한다면 연결(없으면 내부 추론)
    [SerializeField] private MonoBehaviour bottleMainBehaviour; // 예: GlassBottleController
    [SerializeField] private string shatteredMemberName = "isShattered";

    [System.Serializable]
    private struct FragState
    {
        public Vector3 pos;
        public Quaternion rot;
        public Vector3 vel;
        public Vector3 angVel;
    }

    [System.Serializable]
    private struct Data
    {
        public bool active;
        public bool isShattered;
        public Vector3 pos;
        public Quaternion rot;
        public FragState[] frags; // savePhysicsForFragments=true일 때만 채움
        public bool lightEnabled;
        public float lightRange;
        public float lightIntensity;
    }

    public string CaptureState()
    {
        bool shattered = GetIsShattered();
        var d = new Data
        {
            active = gameObject.activeSelf,
            isShattered = shattered,
            pos = transform.position,
            rot = transform.rotation,
            lightEnabled = pointLight ? pointLight.enabled : false,
            lightRange = pointLight ? pointLight.range : 0f,
            lightIntensity = pointLight ? pointLight.intensity : 0f
        };

        if (savePhysicsForFragments && fragmentsParent != null)
        {
            var rbs = fragmentsParent.GetComponentsInChildren<Rigidbody>(includeInactive: true);
            d.frags = new FragState[rbs.Length];
            for (int i = 0; i < rbs.Length; i++)
            {
                var rb = rbs[i];
                d.frags[i] = new FragState
                {
                    pos = rb.transform.position,
                    rot = rb.transform.rotation,
                    vel = rb.velocity,
                    angVel = rb.angularVelocity
                };
            }
        }

        return JsonUtility.ToJson(d);
    }

    public void RestoreState(string json)
    {
        var d = JsonUtility.FromJson<Data>(json);
        gameObject.SetActive(d.active);

        // 위치/회전 복원(TransformSave가 따로 있으면 생략 가능)
        transform.SetPositionAndRotation(d.pos, d.rot);

        // 상태 토글
        ApplyShattered(d.isShattered);

        // 라이트 초기화(프로젝트 규칙: 소리=빛 펄스)
        if (pointLight)
        {
            pointLight.enabled = d.lightEnabled;
            pointLight.range = d.lightRange;
            pointLight.intensity = d.lightIntensity;
        }

        // 파편 물리 복원(선택)
        if (savePhysicsForFragments && fragmentsParent != null && d.frags != null)
        {
            var rbs = fragmentsParent.GetComponentsInChildren<Rigidbody>(includeInactive: true);
            int count = Mathf.Min(rbs.Length, d.frags.Length);
            for (int i = 0; i < count; i++)
            {
                var rb = rbs[i];
                var fs = d.frags[i];
                rb.transform.SetPositionAndRotation(fs.pos, fs.rot);
                rb.velocity = fs.vel;
                rb.angularVelocity = fs.angVel;
            }
        }
    }

    private bool GetIsShattered()
    {
        // 외부 스크립트에 bool이 있으면 그 값을 신뢰
        if (bottleMainBehaviour && !string.IsNullOrEmpty(shatteredMemberName))
        {
            var t = bottleMainBehaviour.GetType();
            var f = t.GetField(shatteredMemberName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(bool))
                return (bool)f.GetValue(bottleMainBehaviour);

            var p = t.GetProperty(shatteredMemberName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (p != null && p.PropertyType == typeof(bool))
                return (bool)p.GetValue(bottleMainBehaviour);
        }

        // 없으면 Hierarchy 상태로 추론
        bool fragmentsOn = fragmentsParent && fragmentsParent.gameObject.activeSelf;
        bool intactOn = intactRoot && intactRoot.activeSelf;
        return fragmentsOn && !intactOn;
    }

    private void ApplyShattered(bool shattered)
    {
        // 온전/파손 토글
        if (intactRoot) intactRoot.SetActive(!shattered);
        if (fragmentsParent) fragmentsParent.gameObject.SetActive(shattered);

        // 충돌/피직스 토글(선택)
        if (redGlassCollider) redGlassCollider.enabled = !shattered;

        // 파손 시 들고 있었다면 강제 해제(선택: FPC가 있으면)
        // var fpc = FindObjectOfType<FirstPersonController>();
        // if (shattered && fpc && fpc.HeldObject != null && fpc.HeldObject.transform.IsChildOf(transform))
        //     fpc.ForceRelease();
    }
}
