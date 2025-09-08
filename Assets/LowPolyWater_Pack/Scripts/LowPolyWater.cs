using System.Collections.Generic;
using UnityEngine;

namespace LowPolyWater
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class LowPolyWater : MonoBehaviour, IElectedReceiver
    {
        // ===== 실시간으로 파도 계산에 쓰이는 값 =====
        [HideInInspector] public float waveHeight = 0.5f;
        [HideInInspector] public float waveFrequency = 0.5f;
        [HideInInspector] public float waveLength = 0.75f;

        [Header("Wave Settings - Start (Idle)")]
        [SerializeField] private float startWaveHeight = 0.5f;
        [SerializeField] private float startWaveFrequency = 0.5f;
        [SerializeField] private float startWaveLength = 0.75f;

        [Header("Wave Settings - Player In Water (Contact)")]
        [SerializeField] private float contactWaveHeight = 1.0f;
        [SerializeField] private float contactWaveFrequency = 1.2f;
        [SerializeField] private float contactWaveLength = 1.0f;

        [Header("Elected Override")]
        [Tooltip("스위치와 연동되는 전기 상태. true일 때 아래 Elected 파라미터로 강제 동작")]
        [SerializeField] private bool isElected = false;
        [SerializeField] private float electedWaveHeight = 1.2f;
        [SerializeField] private float electedWaveFrequency = 1.5f;
        [SerializeField] private float electedWaveLength = 1.1f;

        [Header("Transition")]
        [Tooltip("목표값으로 수렴하는 속도 (클수록 빠름)")]
        [SerializeField] private float transitionSpeed = 3.0f;

        [Header("Player Light Bonus")]
        [Tooltip("플레이어가 물 안에 있을 때 추가되는 라이트 범위(+). 필요 없으면 0")]
        [SerializeField] private float waterRangeBonus = 6f;

        [Header("Trigger Detection")]
        [Tooltip("플레이어 태그명(미지정 시 FPC 존재 여부로도 판별)")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool debugLog = true;

        [Header("Floating Plane Helper (optional)")]
        [Tooltip("Plane이 바닥 위에 떠 있을 때, BoxCollider를 아래로 얼마나 두껍게 늘릴지(m)")]
        [SerializeField] private float extraDownExtent = 0.6f;
        [Tooltip("BoxCollider 최소 Y두께 보장값(m)")]
        [SerializeField] private float minThicknessY = 0.2f;

        // 파도 원점 (기존 구현)
        public Vector3 waveOriginPosition = Vector3.zero;

        // 내부 상태
        private bool playerInside = false;
        private readonly HashSet<FirstPersonController> touchingPlayers = new();

        // 메쉬
        private MeshFilter meshFilter;
        private Mesh runtimeMesh;       // ← 인스턴스별 런타임 메쉬
        private Vector3[] vertices;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;

            startWaveHeight = waveHeight;
            startWaveFrequency = waveFrequency;
            startWaveLength = waveLength;
        }

        private void OnValidate()
        {
            var box = GetComponent<BoxCollider>();
            if (box != null)
            {
                box.isTrigger = true;

                var size = box.size;
                var center = box.center;

                if (size.y < minThicknessY) size.y = minThicknessY;
                center.y -= extraDownExtent * 0.5f;
                size.y += extraDownExtent;

                box.size = size;
                box.center = center;
            }
        }

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();

            // 시작값으로 초기화
            waveHeight = startWaveHeight;
            waveFrequency = startWaveFrequency;
            waveLength = startWaveLength;
        }

        private void Start()
        {
            InitRuntimeMesh();
        }

        private void OnEnable()
        {
            playerInside = false;
            waveHeight = startWaveHeight;
            waveFrequency = startWaveFrequency;
            waveLength = startWaveLength;
        }

        private void InitRuntimeMesh()
        {
            if (meshFilter == null)
            {
                Debug.LogError("[LowPolyWater] MeshFilter 없음 — 시각용 Plane 오브젝트인지 확인하세요.");
                enabled = false;
                return;
            }

            var src = meshFilter.sharedMesh;
            if (src == null)
            {
                Debug.LogError("[LowPolyWater] Mesh가 없습니다. MeshFilter에 유효한 mesh를 할당하세요.");
                enabled = false;
                return;
            }

            // ✅ 각 인스턴스마다 런타임 메쉬 복제
            runtimeMesh = Instantiate(src);
            runtimeMesh.name = src.name + " (LPW Instance)";
            meshFilter.mesh = runtimeMesh;

            // 로우폴리화: 삼각형마다 버텍스 분리
            Vector3[] originalVertices = src.vertices;
            int[] triangles = src.triangles;

            Vector3[] newVerts = new Vector3[triangles.Length];
            for (int i = 0; i < triangles.Length; i++)
            {
                newVerts[i] = originalVertices[triangles[i]];
                triangles[i] = i;
            }

            runtimeMesh.vertices = newVerts;
            runtimeMesh.SetTriangles(triangles, 0);
            runtimeMesh.RecalculateBounds();
            runtimeMesh.RecalculateNormals();

            vertices = runtimeMesh.vertices;

            if (debugLog)
                Debug.Log($"[LowPolyWater] Initialized {name} — verts: {vertices.Length}, mesh: {runtimeMesh.name}");
        }

        // ===== 트리거 감지 =====
        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other)) return;

            var fpc = other.GetComponentInParent<FirstPersonController>();
            if (touchingPlayers.Add(fpc))
            {
                playerInside = true;
                if (debugLog) Debug.Log($"[LowPolyWater] ▶ Player ENTER on {name}");

                if (touchingPlayers.Count == 1 && fpc != null && waterRangeBonus != 0f)
                {
                    fpc.ApplyWaterRangeBonus(waterRangeBonus);
                    if (debugLog) Debug.Log($"[LowPolyWater] Contact ON → Light +{waterRangeBonus}");
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other)) return;

            var fpc = other.GetComponentInParent<FirstPersonController>();
            if (touchingPlayers.Remove(fpc))
            {
                if (debugLog) Debug.Log($"[LowPolyWater] ◀ Player EXIT from {name}");
                if (touchingPlayers.Count == 0)
                {
                    playerInside = false;
                    if (fpc != null && waterRangeBonus != 0f)
                    {
                        fpc.ClearWaterRangeBonus();
                        if (debugLog) Debug.Log("[LowPolyWater] Contact OFF → Light restored");
                    }
                }
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (!playerInside && IsPlayer(other)) playerInside = true;
        }

        private bool IsPlayer(Collider other)
        {
            if (!string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag)) return true;
            return other.GetComponentInParent<FirstPersonController>() != null;
        }

        private void OnDisable()
        {
            foreach (var fpc in touchingPlayers)
            {
                if (fpc != null && waterRangeBonus != 0f) fpc.ClearWaterRangeBonus();
            }
            touchingPlayers.Clear();

            playerInside = false;
            waveHeight = startWaveHeight;
            waveFrequency = startWaveFrequency;
            waveLength = startWaveLength;
        }

        private void Update()
        {
            // 1) 파라미터를 목표값으로 '서서히' 보간
            LerpWaveParams();

            // 2) 파도 적용
            if (runtimeMesh == null || vertices == null) return;
            GenerateWaves();
        }

        private void LerpWaveParams()
        {
            float targetH, targetF, targetL;

            if (isElected)
            {
                // ⚡ 스위치 ON일 때: elected 세트로 고정
                targetH = electedWaveHeight;
                targetF = electedWaveFrequency;
                targetL = electedWaveLength;
            }
            else
            {
                // 기존 로직 유지: 플레이어 접촉 시 contact, 아니면 start
                targetH = playerInside ? contactWaveHeight : startWaveHeight;
                targetF = playerInside ? contactWaveFrequency : startWaveFrequency;
                targetL = playerInside ? contactWaveLength : startWaveLength;
            }

            float k = Time.deltaTime * Mathf.Max(0.001f, transitionSpeed);
            waveHeight = Mathf.Lerp(waveHeight, targetH, k);
            waveFrequency = Mathf.Lerp(waveFrequency, targetF, k);
            waveLength = Mathf.Lerp(waveLength, targetL, k);
        }

        private void GenerateWaves()
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = vertices[i];
                v.y = 0f;

                float distance = Vector3.Distance(v, waveOriginPosition);
                distance = (distance % waveLength) / waveLength;

                v.y = waveHeight * Mathf.Sin(
                    Time.time * Mathf.PI * 2f * waveFrequency +
                    (Mathf.PI * 2f * distance)
                );

                vertices[i] = v;
            }

            runtimeMesh.vertices = vertices;
            runtimeMesh.RecalculateNormals();
            runtimeMesh.MarkDynamic();
        }

        // ===== IElectedReceiver 구현 =====
        public void SetElected(bool value)
        {
            isElected = value;
            if (debugLog) Debug.Log($"[LowPolyWater] SetElected → {isElected}");
        }

#if UNITY_EDITOR
        [ContextMenu("Editor ▸ Force Elected OFF")]
        private void EditorForceOff() => SetElected(false);

        [ContextMenu("Editor ▸ Force Elected ON")]
        private void EditorForceOn() => SetElected(true);
#endif
    }
}
