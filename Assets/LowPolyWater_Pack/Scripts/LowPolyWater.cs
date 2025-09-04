using System.Collections.Generic;
using UnityEngine;

namespace LowPolyWater
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class LowPolyWater : MonoBehaviour
    {
        // ---- 실제 파동에 쓰이는 실시간 값 (GenerateWaves가 참조) ----
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

        [Header("Transition")]
        [Tooltip("파라미터가 목표값으로 수렴하는 속도(Lerp 계수). 높을수록 빨라짐.")]
        [SerializeField] private float transitionSpeed = 3.0f;

        [Header("Player Light Bonus")]
        [Tooltip("플레이어가 물 안에 있을 때 추가되는 라이트 범위(+). 필요 없으면 0.")]
        [SerializeField] private float waterRangeBonus = 6f;

        [Header("Trigger Detection")]
        [Tooltip("플레이어 태그명 (없으면 플레이어 루트의 FirstPersonController로도 탐지함)")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool debugLog = true;

        [Header("Floating Plane Helper (optional)")]
        [Tooltip("Plane이 바닥 위에 떠 있을 때, BoxCollider를 아래로 얼마나 더 두껍게 늘릴지(m)")]
        [SerializeField] private float extraDownExtent = 0.6f;
        [Tooltip("BoxCollider의 최소 Y 두께 보장값(m)")]
        [SerializeField] private float minThicknessY = 0.2f;

        // 파도 원점 (기존 구현 유지)
        public Vector3 waveOriginPosition = Vector3.zero;

        // 내부 상태
        private bool playerInside = false;
        private readonly HashSet<FirstPersonController> touchingPlayers = new();

        // 메시/버텍스 (기존 LowPoly 구현)
        private MeshFilter meshFilter;
        private Mesh mesh;
        private Vector3[] vertices;

        private void Reset()
        {
            // 콜라이더를 트리거로 권장
            var col = GetComponent<Collider>();
            col.isTrigger = true;

            // start 프리셋을 현재 값으로 초기화
            startWaveHeight = waveHeight;
            startWaveFrequency = waveFrequency;
            startWaveLength = waveLength;
        }

        private void OnValidate()
        {
            // 별도 Trigger Volume 없이 Plane 자체로 감지하려면 BoxCollider를 아래로 좀 더 두껍게.
            // (직접 사이즈를 잘 맞췄다면 extraDownExtent=0으로 두어도 됨)
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

            // 시작 시 실시간 값을 start 프리셋으로 맞춤
            waveHeight = startWaveHeight;
            waveFrequency = startWaveFrequency;
            waveLength = startWaveLength;
        }

        private void Start()
        {
            if (meshFilter != null && meshFilter.sharedMesh != null)
                CreateMeshLowPoly(meshFilter);
        }

        private void OnEnable()
        {
            playerInside = false;
            waveHeight = startWaveHeight;
            waveFrequency = startWaveFrequency;
            waveLength = startWaveLength;
        }

        // ===== 트리거 감지: Plane 자신의 BoxCollider를 IsTrigger로 사용 =====
        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other)) return;

            var fpc = other.GetComponentInParent<FirstPersonController>();
            if (touchingPlayers.Add(fpc))
            {
                playerInside = true;
                if (debugLog) Debug.Log($"[LowPolyWater] ▶ Player ENTER on {name}");

                // 첫 진입 시 라이트 보너스 적용
                if (touchingPlayers.Count == 1 && fpc != null)
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
                    if (fpc != null)
                    {
                        fpc.ClearWaterRangeBonus();
                        if (debugLog) Debug.Log("[LowPolyWater] Contact OFF → Light restored");
                    }
                }
            }
        }

        private void OnTriggerStay(Collider other)
        {
            // 드문 프레임 누락 대비
            if (!playerInside && IsPlayer(other)) playerInside = true;
        }

        private bool IsPlayer(Collider other)
        {
            if (!string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag)) return true;
            return other.GetComponentInParent<FirstPersonController>() != null;
        }

        private void OnDisable()
        {
            // 안전 복구 (씬에서 비활성화될 때)
            foreach (var fpc in touchingPlayers)
            {
                if (fpc != null) fpc.ClearWaterRangeBonus();
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

            // 2) 파도 생성(기존 로직)
            if (mesh != null && vertices != null)
                GenerateWaves();
        }

        private void LerpWaveParams()
        {
            float targetH = playerInside ? contactWaveHeight : startWaveHeight;
            float targetF = playerInside ? contactWaveFrequency : startWaveFrequency;
            float targetL = playerInside ? contactWaveLength : startWaveLength;

            float k = Time.deltaTime * Mathf.Max(0.001f, transitionSpeed);
            waveHeight = Mathf.Lerp(waveHeight, targetH, k);
            waveFrequency = Mathf.Lerp(waveFrequency, targetF, k);
            waveLength = Mathf.Lerp(waveLength, targetL, k);
        }

        // ====== 이하: 원래 LowPoly 메쉬 분할 & 파도 생성 코드 ======
        private MeshFilter CreateMeshLowPoly(MeshFilter mf)
        {
            mesh = mf.sharedMesh;
            if (mesh == null) return mf;

            Vector3[] originalVertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            Vector3[] newVerts = new Vector3[triangles.Length];
            for (int i = 0; i < triangles.Length; i++)
            {
                newVerts[i] = originalVertices[triangles[i]];
                triangles[i] = i;
            }

            mesh.vertices = newVerts;
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            vertices = mesh.vertices;
            return mf;
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

            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.MarkDynamic();
            meshFilter.mesh = mesh;
        }
    }
}
