using UnityEngine;

[DisallowMultipleComponent]
public class GlassBottleSave : MonoBehaviour, ISaveable
{
    [Header("Hierarchy Refs")]
    [Tooltip("������ �޽�(�ļ� ��) ��Ʈ")]
    [SerializeField] private GameObject intactRoot;          // ��: ���� �޽�, pointLight ����
    [Tooltip("������� ��� �θ� Transform")]
    [SerializeField] private Transform fragmentsParent;       // ��: "fragments"
    [Tooltip("Red glass �ڽ��ݶ��̴�(���/���� ������)")]
    [SerializeField] private Collider redGlassCollider;       // ��: red_glass(�ɼ�)

    [Header("Optional")]
    [SerializeField] private Light pointLight;                // �ļս� ����Ʈ ȿ���� ���ų� �ʱ�ȭ
    [SerializeField] private bool savePhysicsForFragments = false; // Ǯ ������ �ѱ�

    // �ܺ� ���� ��ũ��Ʈ�� isShattered�� �����Ѵٸ� ����(������ ���� �߷�)
    [SerializeField] private MonoBehaviour bottleMainBehaviour; // ��: GlassBottleController
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
        public FragState[] frags; // savePhysicsForFragments=true�� ���� ä��
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

        // ��ġ/ȸ�� ����(TransformSave�� ���� ������ ���� ����)
        transform.SetPositionAndRotation(d.pos, d.rot);

        // ���� ���
        ApplyShattered(d.isShattered);

        // ����Ʈ �ʱ�ȭ(������Ʈ ��Ģ: �Ҹ�=�� �޽�)
        if (pointLight)
        {
            pointLight.enabled = d.lightEnabled;
            pointLight.range = d.lightRange;
            pointLight.intensity = d.lightIntensity;
        }

        // ���� ���� ����(����)
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
        // �ܺ� ��ũ��Ʈ�� bool�� ������ �� ���� �ŷ�
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

        // ������ Hierarchy ���·� �߷�
        bool fragmentsOn = fragmentsParent && fragmentsParent.gameObject.activeSelf;
        bool intactOn = intactRoot && intactRoot.activeSelf;
        return fragmentsOn && !intactOn;
    }

    private void ApplyShattered(bool shattered)
    {
        // ����/�ļ� ���
        if (intactRoot) intactRoot.SetActive(!shattered);
        if (fragmentsParent) fragmentsParent.gameObject.SetActive(shattered);

        // �浹/������ ���(����)
        if (redGlassCollider) redGlassCollider.enabled = !shattered;

        // �ļ� �� ��� �־��ٸ� ���� ����(����: FPC�� ������)
        // var fpc = FindObjectOfType<FirstPersonController>();
        // if (shattered && fpc && fpc.HeldObject != null && fpc.HeldObject.transform.IsChildOf(transform))
        //     fpc.ForceRelease();
    }
}
