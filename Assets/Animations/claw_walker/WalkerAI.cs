using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class WalkerAI : MonoBehaviour
{
    // ───────── Waypoints / Agent ─────────
    [Header("Waypoints")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private bool loop = true;
    [SerializeField] private float waitTime = 0f;
    [SerializeField] private float arriveThreshold = 0.5f;
    [SerializeField] private bool snapWaypointToNavMesh = true;
    [SerializeField] private float sampleMaxDistance = 2.0f;
    [SerializeField] private int areaMask = NavMesh.AllAreas;

    [Header("Agent")]
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float angularSpeed = 180f;
    [SerializeField] private float acceleration = 8f;

    // ───────── Animation ─────────
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private float speedDamp = 0.15f;
    [SerializeField] private bool applyRootMotion = false;

    // ───────── Footstep Sound ─────────
    [Header("Footstep Sound")]
    [SerializeField] private AudioSource footstepSource;
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private Vector2 pitchRandom = new Vector2(0.97f, 1.03f);
    [SerializeField, Range(0f, 5f)] private float footstepVolume = 1f; // 사용자가 직접 입력 (최대 5)

    // ───────── Visual Yaw Offset (Walk Only) ─────────
    [Header("Visual Yaw Offset (Walk Only)")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float walkYawOffset = 10f;
    [SerializeField] private float yawLerpSpeed = 8f;
    [SerializeField] private float walkSpeedThreshold = 0.2f;

    // ───────── Step Light (Player Pulse Style) ─────────
    [Header("Step Light (Player Pulse Style)")]
    [SerializeField] private Light pointLight;                 // 라이트 A
    [SerializeField] private Light pointLight2;                // 라이트 B
    [SerializeField] private float maxLightRange = 5f;
    [SerializeField] private float pointLightIntensity = 7f;
    [SerializeField] private float lightDuration = 0.5f;
    [SerializeField] private float runExtraRange = 0f; // 필요시 달리기 상태 연동

    // ───────── Breathing Audio ─────────
    [Header("Breathing Audio")]
    [SerializeField] private AudioSource breathingSource;
    [SerializeField] private AudioClip breathingClip;
    [SerializeField] private bool loopBreathing = true;
    [SerializeField, Range(0f, 2f)] private float breathingVolume = 0.6f;

    [Header("Scream Audio (Rage Only)")]
    [SerializeField] private AudioSource screamSource;
    [SerializeField] private AudioClip screamClip;
    [SerializeField, Range(0f, 2f)] private float screamVolume = 0.9f;
    [SerializeField] private bool loopScream = true;

    // ───────── Step Gate ─────────
    [Header("Step Gate")]
    [SerializeField] private float minStepInterval = 0.18f;
    [SerializeField] private float stepMinMoveSpeed = 0.2f;

    // ───────── Awareness States ─────────
    private enum WalkerState { Patrol, Search, Rage }

    [Header("Search Settings")]
    [SerializeField] private float searchToRageDelay = 1.0f; // 검색 후 Rage로 넘어가는 지연(초)
    private float searchTimer = 0f;

    [Header("Rage Settings")]
    [SerializeField] private WalkerState state = WalkerState.Patrol;
    [SerializeField] private float rageTriggerWaveRange = 12f;   // 트리거 파동 임계
    [SerializeField] private float rageLightHoldOffset = -3f;    // 고정 범위 = maxLightRange + offset
    [SerializeField] private float playerMoveSecondsToChase = 2f;// Rage에서 플레이어가 이 시간 이상 움직이면 추적
    [SerializeField] private float chaseSpeed = 4.5f;            // 추적 속도(4 이상)
    [SerializeField] private float requeryFootstepInterval = 1f; // 1초 후 재평가
    [SerializeField] private float rageForgetAfter = 3f;         // 추가 감지 없으면 Rage 해제
    [SerializeField] private float playerMoveThresh = 0.05f;     // "움직임" 판정

    [Header("Run Tuning (Rage)")]
    [SerializeField] private float rageAngularSpeed = 7200f;   // 회전 즉시 반응에 가깝게
    [SerializeField] private float rageAcceleration = 50f;     // 가속 빠르게
    [SerializeField] private bool facePlayerEveryFrame = true; // 매 프레임 얼굴을 플레이어로

    [Header("Search Visuals")]
    [SerializeField] private Color searchFromColor = Color.white; // Patrol 시 색 (복귀 색)
    [SerializeField] private Color searchColor = Color.red;    // Search/Rage 시 색
    [SerializeField] private float searchColorFade = 0.4f;        // 색 전환 시간(초)


    [Header("Player Catch (Game Over)")]
    [SerializeField] private float gameOverDistance = 0.5f; // Rage 중 이 거리 이하면 GameOver

    private float savedAngularSpeed;
    private float savedAcceleration;

    private Coroutine screamFadeCo;

    [SerializeField] private Transform spawnAnchor;   // 선택: 지정 시 여기로 복귀
    [SerializeField] private bool useNearestWaypointAsSpawn = true; // 앵커 없으면 웨이포인트 근처 선택
    private Vector3 _initialSpawnPos;
    private Quaternion _initialSpawnRot;
    [SerializeField] private string idleStateName = "Idle";         // 안전용(없어도 동작)
    [SerializeField] private string walkStateName = "Walk";         // 안전용(없어도 동작)



    // 내부 상태
    private Coroutine lightColorCo;

    // ───────── Internals ─────────
    private NavMeshAgent agent;
    private int currentIndex = 0;
    private float waitTimer = 0f;
    private float lastStepTime = -999f;
    private NavMeshPath tmpPath;

    private Coroutine lightCoroutine;   // pointLight용
    private Coroutine lightCoroutine2;  // pointLight2용

    // Player refs
    private FirstPersonController player;
    private Transform playerT;
    private Vector3 prevPlayerPos;
    private float playerMoveTimer = 0f;
    private float lastFootstepHeardTime = -999f;
    private float lastPlayerLightRange = 0f;
    private float nextChaseAllowedTime = 0f;
    private Vector3 lastHeardPlayerPos;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();
        tmpPath = new NavMeshPath();

        if (agent)
        {
            agent.speed = walkSpeed;
            agent.angularSpeed = angularSpeed;
            agent.acceleration = acceleration;
            agent.stoppingDistance = Mathf.Max(0.05f, arriveThreshold);
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.autoBraking = true;
            agent.autoRepath = true;
        }
        if (animator) animator.applyRootMotion = applyRootMotion;

        if (pointLight) { pointLight.range = 0f; pointLight.intensity = 0f; }
        if (pointLight2) { pointLight2.range = 0f; pointLight2.intensity = 0f; }

        if (breathingSource != null)
        {
            breathingSource.playOnAwake = false;
            breathingSource.loop = loopBreathing;
            breathingSource.volume = breathingVolume;
            if (breathingClip != null && breathingSource.clip == null)
                breathingSource.clip = breathingClip;
        }

        if (screamSource != null)
        {
            screamSource.playOnAwake = false;
            screamSource.loop = loopScream;
            screamSource.volume = screamVolume;
            if (screamClip != null && screamSource.clip == null)
                screamSource.clip = screamClip;
        }

        if (spawnAnchor != null)
        {
            _initialSpawnPos = spawnAnchor.position;
            _initialSpawnRot = spawnAnchor.rotation;
        }
        else
        {
            _initialSpawnPos = transform.position;
            _initialSpawnRot = transform.rotation;
        }
    }

    private void OnEnable()
    {
        SaveSystem.AfterLoad += OnAfterLoad_ResetWalker;
    }

    private void OnDisable()
    {
        SaveSystem.AfterLoad -= OnAfterLoad_ResetWalker;
    }

    private void Start()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning("[WalkerAI] Waypoints가 비어 있습니다.");
            enabled = false;
            return;
        }

        GoToNearestWaypointReachable();

        if (breathingSource && breathingClip && !breathingSource.isPlaying)
            breathingSource.Play();

        player = FindObjectOfType<FirstPersonController>();
        if (player != null) { playerT = player.transform; prevPlayerPos = playerT.position; }
    }

    private void Update()
    {
        // 항상 플레이어 파동/상태를 먼저 확인
        HandleAwarenessStates();

        // 애니메이션 속도 갱신
        if (animator && agent != null)
        {
            float spd = agent.velocity.magnitude;
            animator.SetFloat(speedParam, spd, speedDamp, Time.deltaTime);
        }

        // 상태별 로직
        if (state == WalkerState.Patrol)
        {
            HandlePatrolAdvance();
        }
        else if (state == WalkerState.Search)
        {
            // Search: 완전 정지 + 타이머
            StopAgentHard();
            searchTimer += Time.deltaTime;
            if (searchTimer >= searchToRageDelay)
            {
                EnterRage();
            }
        }
        else // Rage
        {
            // Rage는 HandleAwarenessStates()에서 유지/추적/해제를 처리함
        }
    }

    private void LateUpdate()
    {
        ApplyWalkYawOffset();
    }

    // ───────── Patrol Core ─────────
    private void HandlePatrolAdvance()
    {
        if (agent == null) return;
        if (state != WalkerState.Patrol)
        {
            Debug.Log("[WALKER] ⛔ Patrol 차단 (state=" + state + ")");
            return;
        }

        if (!agent.hasPath || agent.pathStatus == NavMeshPathStatus.PathPartial)
        {
            if (!SetDestinationToWaypoint(currentIndex))
                AdvanceToNextReachable();
        }

        if (agent.pathPending) return;

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTime)
            {
                AdvanceToNextReachable();
                waitTimer = 0f;
            }
        }
        else
        {
            waitTimer = 0f;
        }
    }

    private bool SetDestinationToWaypoint(int index)
    {
        if (state != WalkerState.Patrol) return false;
        if (waypoints == null || waypoints.Length == 0) return false;
        if (index < 0 || index >= waypoints.Length) return false;
        if (waypoints[index] == null) return false;

        Vector3 target = waypoints[index].position;

        if (snapWaypointToNavMesh)
        {
            if (!NavMesh.SamplePosition(target, out NavMeshHit hit, sampleMaxDistance, areaMask))
                return false;

            target = hit.position;

            if (!NavMesh.CalculatePath(transform.position, target, areaMask, tmpPath) ||
                tmpPath.status != NavMeshPathStatus.PathComplete)
                return false;
        }

        agent.isStopped = false;
        agent.speed = walkSpeed;
        agent.SetDestination(target);
        return true;
    }

    private bool AdvanceToNextReachable()
    {
        if (waypoints == null || waypoints.Length == 0) return false;

        int tries = waypoints.Length;
        int i = currentIndex;
        for (int t = 0; t < tries; t++)
        {
            int nextIdx = (i + 1) % waypoints.Length;
            if (SetDestinationToWaypoint(nextIdx))
            {
                currentIndex = nextIdx;
                return true;
            }
            i = nextIdx;
        }
        return false;
    }

    private void GoToNearestWaypointReachable()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        int nearest = -1;
        float best = float.MaxValue;

        for (int i = 0; i < waypoints.Length; i++)
        {
            var t = waypoints[i];
            if (!t) continue;
            float d = (t.position - transform.position).sqrMagnitude;
            if (d < best) { best = d; nearest = i; }
        }

        if (nearest >= 0 && SetDestinationToWaypoint(nearest))
        {
            currentIndex = nearest;
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                AdvanceToNextReachable();
            return;
        }

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (SetDestinationToWaypoint(i))
            {
                currentIndex = i;
                return;
            }
        }
    }

    // ───────── Visual Yaw Offset ─────────
    private void ApplyWalkYawOffset()
    {
        if (visualRoot == null) return;

        // Rage 상태에서는 시각 Yaw 오프셋을 "즉시" 비활성화하고 본체 회전에 스냅
        if (state == WalkerState.Rage)
        {
            visualRoot.rotation = transform.rotation; // ← 즉시
            return;
        }

        // (기존) Idle/Run 구분치 대신, "달리기 근사"를 느슨하게 감지하고 빠르게 줄이기
        float animSpeed = animator ? animator.GetFloat(speedParam) : 0f;
        bool isRunLike = animSpeed >= 3.5f; // 달리기/전력질주 근사

        if (isRunLike)
        {
            // 달리기 때도 스냅(보간 X)로 원래 회전으로 붙임
            visualRoot.rotation = transform.rotation;
            return;
        }

        bool isWalking = agent && agent.velocity.sqrMagnitude > (walkSpeedThreshold * walkSpeedThreshold);

        Vector3 moveDir = transform.forward;
        if (agent && agent.desiredVelocity.sqrMagnitude > 0.001f)
            moveDir = agent.desiredVelocity.normalized;

        Vector3 biasedDir = Quaternion.AngleAxis(walkYawOffset, Vector3.up) * moveDir;

        Quaternion targetWorldRot = isWalking
            ? Quaternion.LookRotation(biasedDir, Vector3.up)
            : transform.rotation;

        visualRoot.rotation = Quaternion.Slerp(
            visualRoot.rotation,
            targetWorldRot,
            Mathf.Clamp01(Time.deltaTime * yawLerpSpeed)
        );
    }

    // ───────── Footstep & Light ─────────
    public void OnStep() // 애니메이션 이벤트로 호출
    {
        if (!CanPlayStep()) return;

        // 1) 발걸음 사운드 (사용자 입력 볼륨)
        PlayFootstepSound();

        // 2) Rage 중엔 빛 펄스 금지(고정 유지)
        if (state == WalkerState.Rage) return;

        // 3) 평소엔 플레이어와 동일한 서서히 증가→감소 (두 라이트 모두)
        float extra = 0f; // 필요 시 runExtraRange 사용
        if (pointLight)
        {
            if (lightCoroutine != null) StopCoroutine(lightCoroutine);
            lightCoroutine = StartCoroutine(PulseLightEffect(lightDuration, extra + runExtraRange, pointLightIntensity, pointLight));
        }
        if (pointLight2)
        {
            if (lightCoroutine2 != null) StopCoroutine(lightCoroutine2);
            lightCoroutine2 = StartCoroutine(PulseLightEffect(lightDuration, extra + runExtraRange, pointLightIntensity, pointLight2));
        }
    }

    private bool CanPlayStep()
    {
        if (Time.time - lastStepTime < minStepInterval) return false;
        if (agent == null) { lastStepTime = Time.time; return true; }
        if (agent.isStopped || agent.pathPending) return false;
        if (agent.velocity.sqrMagnitude < stepMinMoveSpeed * stepMinMoveSpeed) return false;
        lastStepTime = Time.time;
        return true;
    }

    private void PlayFootstepSound()
    {
        if (!footstepSource || footstepClips == null || footstepClips.Length == 0) return;

        float min = Mathf.Min(pitchRandom.x, pitchRandom.y);
        float max = Mathf.Max(pitchRandom.x, pitchRandom.y);
        footstepSource.pitch = Random.Range(min, max);

        var clip = footstepClips[Random.Range(0, footstepClips.Length)];
        footstepSource.PlayOneShot(clip, Mathf.Clamp(footstepVolume, 0f, 5f));
    }

    // ───────── PulseLightEffect (플레이어와 동일) ─────────
    private IEnumerator PulseLightEffect(float duration, float extraRange, float intensity, Light point)
    {
        if (point == null) yield break;

        float halfDuration = duration / 2f;
        float timer = 0f;
        float startRange = point.range;
        float startIntensity = point.intensity;
        float targetRange = maxLightRange + extraRange;

        // Light 증가
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = timer / halfDuration;
            point.range = Mathf.Lerp(startRange, targetRange, t);
            point.intensity = Mathf.Lerp(startIntensity, intensity, t);
            yield return null;
        }

        // Light 감소
        timer = 0f;
        startRange = point.range;
        startIntensity = point.intensity;

        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = timer / halfDuration;
            point.range = Mathf.Lerp(startRange, 0f, t);
            point.intensity = Mathf.Lerp(startIntensity, 0f, t);
            yield return null;
        }

        point.range = 0f;
        point.intensity = 0f;
    }

    // ───────── Awareness / Search / Rage ─────────
    private void HandleAwarenessStates()
    {
        if (player == null || playerT == null) return;

        float pRange = player.GetPointLightRange(); // 플레이어 빛 파동
        float distToPlayer = Vector3.Distance(transform.position, playerT.position);

        // 발소리(파동) 상승 엣지 감지
        bool heardFootstepNow = false;
        if (pRange > lastPlayerLightRange && pRange >= 1f)
            heardFootstepNow = true;
        lastPlayerLightRange = pRange;

        // ─ Patrol: 트리거 만족 시 Search로
        if (state == WalkerState.Patrol)
        {
            if (pRange >= rageTriggerWaveRange && distToPlayer <= pRange)
            {
                EnterSearch();
            }
            return;
        }

        // ─ Search: 상태 유지(정지), 타이머는 Update에서 진행
        if (state == WalkerState.Search)
        {
            // 검색 중에도 마지막 발소리 위치는 계속 갱신
            if (heardFootstepNow)
            {
                lastHeardPlayerPos = playerT.position;
                lastFootstepHeardTime = Time.time;
            }
            return;
        }

        // HandleAwarenessStates() 안 Rage 부분
        if (state == WalkerState.Rage)
        {
            // 🔥 외부 명령 오버라이드: 웅크림/정지와 관계없이 즉시 추격
            if (Time.time < externalChaseOverrideEnd)
            {
                if (playerT) SnapRunTo(playerT.position);
                GameOverIfTouchingPlayer();
                return; // 이하의 '웅크림이면 대기' 로직 우회
            }

            bool playerIsCrouching = player != null && player.IsPlayerCrouching;

            if (!playerIsCrouching)
            {
                // 🚀 플레이어가 걷거나 달리는 중이면 → 즉시 추적
                SnapRunTo(playerT.position);
            }
            else
            {
                // 🕵️ 플레이어가 앉아있으면 기존 방식 유지 (빛/발소리 추적)
                if ((playerT.position - prevPlayerPos).magnitude > playerMoveThresh)
                    playerMoveTimer += Time.deltaTime;
                else
                    playerMoveTimer = 0f;
                prevPlayerPos = playerT.position;

                if (pRange > lastPlayerLightRange && pRange >= 1f)
                {
                    lastHeardPlayerPos = playerT.position;
                    lastFootstepHeardTime = Time.time;
                }

                if (playerMoveTimer >= playerMoveSecondsToChase && Time.time >= nextChaseAllowedTime)
                {
                    SnapRunTo(lastHeardPlayerPos);
                    nextChaseAllowedTime = Time.time + requeryFootstepInterval;
                }

                if (Time.time >= nextChaseAllowedTime && Time.time - lastFootstepHeardTime <= requeryFootstepInterval)
                {
                    SnapRunTo(lastHeardPlayerPos);
                    nextChaseAllowedTime = Time.time + requeryFootstepInterval;
                }

                if (Time.time - lastFootstepHeardTime > rageForgetAfter)
                {
                    ExitRage();
                }
            }

            GameOverIfTouchingPlayer();
        }

    }

    private void EnterSearch()
    {
        state = WalkerState.Search;
        searchTimer = 0f;

        StopAgentHard();

        lastFootstepHeardTime = Time.time;
        nextChaseAllowedTime = Time.time;
        if (playerT) { lastHeardPlayerPos = playerT.position; prevPlayerPos = playerT.position; }
        playerMoveTimer = 0f;

        // ✨ Search 색으로 전환
        StartLightColorFade(searchColor);
        if (player != null) player.BeginThreatTint(searchColorFade); // 플레이어 라이트도 서서히 빨강
    }


    private void EnterRage()
    {
        state = WalkerState.Rage;

        // 즉시 완전 정지
        StopAgentHard();

        // 빛 고정: maxLightRange - 3 (기존 유지)
        float holdRange = Mathf.Max(0f, maxLightRange + rageLightHoldOffset);
        if (pointLight)
        {
            if (lightCoroutine != null) StopCoroutine(lightCoroutine);
            pointLight.range = holdRange;
            pointLight.intensity = pointLightIntensity;
        }
        if (pointLight2)
        {
            if (lightCoroutine2 != null) StopCoroutine(lightCoroutine2);
            pointLight2.range = holdRange;
            pointLight2.intensity = pointLightIntensity;
        }

        // 오디오 전환
        StopBreathing();
        PlayScream();

        // 플레이어 추적 초기화
        lastFootstepHeardTime = Time.time;
        nextChaseAllowedTime = Time.time;
        if (playerT) { lastHeardPlayerPos = playerT.position; prevPlayerPos = playerT.position; }
        playerMoveTimer = 0f;

        // ★ Rage용 에이전트 즉시 회전/가속 튜닝
        if (agent != null)
        {
            savedAngularSpeed = agent.angularSpeed;
            savedAcceleration = agent.acceleration;

            agent.autoBraking = false;                 // 급정지로 인한 방향지연 방지
            agent.angularSpeed = rageAngularSpeed;     // 즉시 꺾임에 가깝게
            agent.acceleration = rageAcceleration;     // 빠른 가속
            agent.speed = Mathf.Max(chaseSpeed, 4f);
            agent.isStopped = false;
        }

        // ★ 시각 Yaw 오프셋 즉시 OFF (스냅)
        if (visualRoot) visualRoot.rotation = transform.rotation;
    }


    private void ExitRage()
    {
        state = WalkerState.Patrol;

        StopScream();
        PlayBreathing();

        if (pointLight)
        {
            if (lightCoroutine != null) StopCoroutine(lightCoroutine);
        }
        if (pointLight2)
        {
            if (lightCoroutine2 != null) StopCoroutine(lightCoroutine2);
        }

        // 순찰 재개 및 에이전트 값 복원
        if (agent != null)
        {
            agent.isStopped = false;
            agent.speed = walkSpeed;
            agent.autoBraking = true;                // 복원
            agent.angularSpeed = savedAngularSpeed;  // 복원
            agent.acceleration = savedAcceleration;  // 복원
        }
        GoToNearestWaypointReachable();

        // 색상 복귀
        StartLightColorFade(searchFromColor);
        if (player != null) player.EndThreatTint(searchColorFade);
    }


    private void SnapRunTo(Vector3 pos)
    {
        if (agent == null) return;
        agent.speed = Mathf.Max(chaseSpeed, 4f); // 최소 4 이상
        agent.isStopped = false;
        agent.SetDestination(pos);
    }

    private void StopAgentHard()
    {
        if (!agent) return;

        // 이동 업데이트 중단 + 남아있는 경로/속도 제거
        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;

        // 🔒 같은 프레임에 내부 위치 버퍼까지 고정해서 잔여 보정 이동 차단
        agent.nextPosition = transform.position;

        // 필요시 더 강하게 고정하고 싶다면 아래 주석 해제
        // agent.Warp(transform.position);
    }

    // ───────── Gizmos ─────────
    private void OnDrawGizmosSelected()
    {
        if (waypoints != null && waypoints.Length > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (!waypoints[i]) continue;
                Gizmos.DrawSphere(waypoints[i].position, 0.08f);
                if (i < waypoints.Length - 1 && waypoints[i + 1])
                    Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                else if (i == waypoints.Length - 1 && loop && waypoints[0])
                    Gizmos.DrawLine(waypoints[i].position, waypoints[0].position);
            }
        }
    }

    private void StartLightColorFade(Color target)
    {
        if (lightColorCo != null) StopCoroutine(lightColorCo);
        lightColorCo = StartCoroutine(FadeLightColorRoutine(target, searchColorFade));
    }

    private IEnumerator FadeLightColorRoutine(Color target, float duration)
    {
        if (duration <= 0f) duration = 0.001f;

        Color start1 = pointLight ? pointLight.color : Color.white;
        Color start2 = pointLight2 ? pointLight2.color : Color.white;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            if (pointLight) pointLight.color = Color.Lerp(start1, target, t);
            if (pointLight2) pointLight2.color = Color.Lerp(start2, target, t);
            yield return null;
        }

        if (pointLight) pointLight.color = target;
        if (pointLight2) pointLight2.color = target;
        lightColorCo = null;
    }


    private void GameOverIfTouchingPlayer()
    {
        if (player == null || playerT == null)
        {
            player = FindObjectOfType<FirstPersonController>();
            if (player != null) playerT = player.transform;
            if (playerT == null) return;
        }

        Vector3 a = transform.position;
        Vector3 b = playerT.position;
        float distXZ = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        if (distXZ <= gameOverDistance)
        {
            var finisher = GetComponent<WalkerGameOverFinisher>(); // 👈 적 전용 컴포넌트
            if (finisher != null)
                player.TriggerGameOver(finisher);
            else
                player.TriggerGameOver("CaughtByWalker"); // 안전장치
        }
    }

    private void PlayBreathing()
    {
        if (breathingSource == null || breathingClip == null) return;
        if (!breathingSource.isPlaying)
        {
            breathingSource.loop = loopBreathing;
            breathingSource.volume = breathingVolume;
            breathingSource.clip = breathingClip;
            breathingSource.Play();
        }
    }

    private void StopBreathing()
    {
        if (breathingSource != null && breathingSource.isPlaying)
            breathingSource.Stop();
    }

    private void PlayScream()
    {
        if (screamSource == null || screamClip == null) return;
        // 재진입 시에도 바로 울부짖음을 보장
        screamSource.loop = loopScream;
        screamSource.volume = screamVolume;
        screamSource.clip = screamClip;
        if (!screamSource.isPlaying) screamSource.Play();
    }

    private void StopScream()
    {
        if (screamSource != null && screamSource.isPlaying)
            screamSource.Stop();
    }

    public void StopRageScream()
    {
        // 내부의 private StopScream()을 그대로 호출
        // (screamSource.Stop()을 직접 써도 무방)
        // StopScream();
        if (screamSource != null && screamSource.isPlaying) screamSource.Stop();
    }
    /// <summary>rage 비명을 부드럽게 꺼준다.</summary>
    public void FadeOutRageScream(float seconds = 0.6f)
    {
        if (screamSource == null) return;

        if (screamFadeCo != null) StopCoroutine(screamFadeCo);
        screamFadeCo = StartCoroutine(FadeOutScreamCo(seconds));
    }

    private IEnumerator FadeOutScreamCo(float seconds)
    {
        if (screamSource == null) yield break;

        float dur = Mathf.Max(0.001f, seconds);
        float startVol = screamSource.volume;

        // 재생 중이 아니어도 볼륨만 0으로 내려도 안전
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            screamSource.volume = Mathf.Lerp(startVol, 0f, t);
            yield return null;
        }

        // 완전히 정지 + 볼륨 복원
        if (screamSource.isPlaying) screamSource.Stop();
        screamSource.volume = startVol;
        screamFadeCo = null;
    }

    // WalkerAI.cs — 클래스 내부 어딘가(예: 하단) 추가

    [Header("External Command")]
    [SerializeField] private float externalChaseOverrideSeconds = 2f;
    private float externalChaseOverrideEnd = -999f;

    /// <summary>
    /// 외부(미믹 등)에서 강제 Rage + 즉시 추격을 명령. overrideSeconds 동안 플레이어 상태 무시.
    /// </summary>
    public void ForceEnterRageAndChase(Transform target, bool snapImmediate = true, float overrideSeconds = -1f)
    {
        // ✅ 수신 로그
        Debug.Log($"[WALKER:{name}] <SIGNAL> Received from Mimic. target={(target ? target.name : "null")}, snap={snapImmediate}, override={(overrideSeconds > 0f ? overrideSeconds : externalChaseOverrideSeconds)}s");

        if (!target) return;

        // 플레이어 참조 보강
        if (player == null) player = FindObjectOfType<FirstPersonController>();
        playerT = (player != null) ? player.transform : target;

        // Rage 진입 및 초기화(사운드/라이트/에이전트 튜닝 포함)
        Debug.Log($"[WALKER:{name}] EnterRage() by external signal");
        StartLightColorFade(searchColor);
        if (player != null) player.BeginThreatTint(searchColorFade);

        EnterRage(); // 기존 코드 그대로

        // 즉시 목적지 설정
        if (playerT != null)
        {
            lastHeardPlayerPos = playerT.position;
            prevPlayerPos = playerT.position;
            if (snapImmediate)
            {
                Debug.Log($"[WALKER:{name}] SnapRunTo({playerT.position})");
                SnapRunTo(playerT.position);
            }
        }

        // 상태 무시 오버라이드 시간 설정
        float dur = (overrideSeconds > 0f) ? overrideSeconds : externalChaseOverrideSeconds;
        externalChaseOverrideEnd = Time.time + dur;
        Debug.Log($"[WALKER:{name}] Forced-chase window: {dur:0.00}s (until t={externalChaseOverrideEnd:0.00})");
    }


    /// <summary>
    /// 체크포인트 로드 직후 Walker를 안전한 순찰상태로 초기화한다.
    /// - killzone에 갇힘 방지
    /// - NavMeshAgent 경로/속도/버퍼 초기화
    /// - Rage/사운드/라이트 정리
    /// - 가장 가까운 웨이포인트로 이동 재개
    /// </summary>
    /// 
    [SerializeField] private bool debugFindPlayerLogs = true;

    // 안전 로그 유틸
    private void LogDbg(string msg)
    {
        if (debugFindPlayerLogs) Debug.Log($"[WalkerAI:{name}] {msg}");
    }
    private void WarnDbg(string msg)
    {
        if (debugFindPlayerLogs) Debug.LogWarning($"[WalkerAI:{name}] {msg}");
    }
    private void OnAfterLoad_ResetWalker()
    {

        if (player == null)
        {
            var fpc = FindObjectOfType<FirstPersonController>();
            if (fpc != null)
            {
                player = fpc;
                playerT = fpc.transform;
                prevPlayerPos = playerT.position;
                Debug.Log("[WalkerAI] AfterLoad → 플레이어 참조 재획득 완료: " + fpc.name);
            }
            else
            {
                Debug.LogWarning("[WalkerAI] AfterLoad → 플레이어를 찾지 못했습니다.");
            }
        }
        // ── B) 라이트/오디오 mute 해제 (킬캠에서 꺼놨을 수 있음)
        RestoreLightsSafe();
        ForcePatrolVisualsOnLoad();
        // ── C) NavMeshAgent 완전 리셋 + 안전 워프
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;

            Vector3 targetPos = _initialSpawnPos;
            Quaternion targetRot = _initialSpawnRot;

            if (useNearestWaypointAsSpawn && waypoints != null && waypoints.Length > 0)
            {
                int nearest = -1; float best = float.MaxValue;
                for (int i = 0; i < waypoints.Length; i++)
                {
                    var t = waypoints[i]; if (!t) continue;
                    float d = (t.position - transform.position).sqrMagnitude;
                    if (d < best) { best = d; nearest = i; }
                }
                if (nearest >= 0)
                {
                    targetPos = waypoints[nearest].position;
                    var next = waypoints[(nearest + 1) % waypoints.Length];
                    if (next) targetRot = Quaternion.LookRotation((next.position - targetPos).normalized, Vector3.up);
                }
            }

#if UNITY_2021_3_OR_NEWER
        agent.Warp(targetPos);
#else
            transform.position = targetPos;
            agent.nextPosition = targetPos;
#endif
            transform.rotation = targetRot;

            // NavMeshAgent 기본값 복원
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.isStopped = false;
        }

        // ── D) 애니메이터 재활성화/재바인드 + 이동 파라미터 세팅
        EnsureAnimatorAlive();

        // ── E) 즉시 순찰 재개(목표 지정)
        ResumePatrolFromHere();

        Debug.Log($"[WalkerAI] AfterLoad Reset → pos={transform.position} ; resume patrol/anim/audio");
        AfterLoad_DebugReacquirePlayer();
    }

    private void EnsureAnimatorAlive()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) return;

        if (!animator.enabled) animator.enabled = true;

        // 런타임컨트롤러가 날아간 케이스 대비
        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning("[WalkerAI] Animator has no controller after load.");
            return;
        }

        // 트리거나 레이어 꼬임 방지
        animator.Rebind();
        animator.Update(0f);

        // Speed 파라미터 기반 블렌드라면 0→보행 속도로 세팅
        if (!string.IsNullOrEmpty(speedParam))
        {
            // 에이전트 속도 기준으로 세팅 (없으면 1로 보행 강제)
            float spd = (agent != null) ? Mathf.Max(0.5f, agent.speed) : 1f;
            animator.SetFloat(speedParam, spd, 0.05f, 0f);
        }

        // 상태 이름이 있는 경우 안전하게 크로스페이드 시도(없어도 에러 안나도록 try)
        try
        {
            if (!string.IsNullOrEmpty(walkStateName))
            {
                animator.CrossFadeInFixedTime(walkStateName, 0.1f);
            }
        }
        catch { /* 애니메이터에 해당 스테이트 없으면 무시 */ }
    }

    private void ResumePatrolFromHere()
    {
        // 웨이포인트 시스템에 맞춰 목적지 재설정
        if (agent == null) return;

        Transform target = GetNextWaypointFromCurrentPosition();
        if (target != null)
        {
            agent.ResetPath();
            agent.isStopped = false;
            agent.SetDestination(target.position);
        }

        // 애니메이션 파라미터도 한번 더 보행 쪽으로 보정
        if (!string.IsNullOrEmpty(speedParam) && animator != null)
        {
            float spd = Mathf.Max(0.5f, agent.speed);
            animator.SetFloat(speedParam, spd, 0.05f, 0f);
        }
    }

    private Transform GetNextWaypointFromCurrentPosition()
    {
        if (waypoints == null || waypoints.Length == 0) return null;

        // 가장 가까운 웨이포인트를 찾아 그 다음 웨이포인트로 향하게 하면 “순찰 재개” 느낌
        int nearest = -1; float best = float.MaxValue;
        for (int i = 0; i < waypoints.Length; i++)
        {
            var t = waypoints[i]; if (!t) continue;
            float d = (t.position - transform.position).sqrMagnitude;
            if (d < best) { best = d; nearest = i; }
        }

        if (nearest < 0) return waypoints[0];

        int next = (nearest + 1) % waypoints.Length;
        return waypoints[next] ? waypoints[next] : waypoints[nearest];
    }

    private void RestoreLightsSafe()
    {
        // 킬캠에서 killcamDisableLights 끈 경우가 있어 복구
        var lights = GetComponentsInChildren<Light>(true);
        foreach (var l in lights)
        {
            if (!l) continue;
            // Whisper의 기본 포인트라이트라면 최소 상태로만 되돌림(범위 0, intensity 0)
            // 필요시 여기서 프로젝트 규칙에 맞는 기본값으로 조정
            if (l.type == LightType.Point)
            {
                l.enabled = true;
                // 범위/강도는 이동 중 발걸음/상태에 의해 다시 제어
            }
        }
    }

    // 내부 FSM이 있다면 Patrol로 강제(없으면 빈 함수로 둬도 OK)
    private void ForcePatrolVisualsOnLoad()
    {
        // 상태는 이미 Patrol로 돌려놨다고 가정
        // (만약 아닐 수도 있으면 아래 한 줄을 남겨둡니다)
        state = WalkerState.Patrol;

        // Rage 사운드 정리, 기본 호흡 재개
        StopScream();
        PlayBreathing();

        // Rage에서 고정되어 있던 라이트 즉시 초기화
        if (pointLight)
        {
            if (lightCoroutine != null) StopCoroutine(lightCoroutine);
            pointLight.color = searchFromColor; // Patrol 기본색으로
            pointLight.range = 0f;
            pointLight.intensity = 0f;
        }
        if (pointLight2)
        {
            if (lightCoroutine2 != null) StopCoroutine(lightCoroutine2);
            pointLight2.color = searchFromColor;
            pointLight2.range = 0f;
            pointLight2.intensity = 0f;
        }

        // 추격/재평가 타이밍 리셋(감지 재개 보조)
        nextChaseAllowedTime = 0f;
        lastFootstepHeardTime = -999f;
        lastPlayerLightRange = 0f; // 엣지 감지 재시작을 위해 초기화
    }

    private void AfterLoad_DebugReacquirePlayer()
    {
        LogDbg("AfterLoad: begin player reacquire");

        // 활성 오브젝트에서 우선 탐색
        var activeFpc = FindObjectOfType<FirstPersonController>();
        // 비활성 포함 전체(씬 밖/비활성 포함)
        var allFpcs = Resources.FindObjectsOfTypeAll<FirstPersonController>();
        int allCount = allFpcs != null ? allFpcs.Length : 0;

        LogDbg($"AfterLoad: activeFpc={(activeFpc ? activeFpc.name : "null")}, allFpcs.count={allCount}");

        if (allCount > 0)
        {
            for (int i = 0; i < allFpcs.Length; i++)
            {
                var f = allFpcs[i];
                if (!f) continue;
                var go = f.gameObject;
                LogDbg($"  - FPC[{i}] name={go.name}, activeInHierarchy={go.activeInHierarchy}, activeSelf={go.activeSelf}, scene={go.scene.name}");
            }
        }

        if (activeFpc != null)
        {
            player = activeFpc;
            playerT = player.transform;
            prevPlayerPos = playerT.position;
            LogDbg($"AfterLoad: ACTIVE FPC re-acquired → {player.name} (scene={player.gameObject.scene.name})");
        }
        else
        {
            WarnDbg("AfterLoad: active FPC not found this frame → start scan coroutine");
            StartCoroutine(Co_DebugScanPlayerAfterLoad());
        }
    }

    // ─────────────────────────────────────────────
    // ② 몇 프레임 동안 주사하며 로그만 찍는 코루틴(동작 변경 없음)
    // ─────────────────────────────────────────────
    private IEnumerator Co_DebugScanPlayerAfterLoad()
    {
        float t0 = Time.time;
        const float timeout = 2.0f; // 최대 2초(원하면 줄여도 됨)
        int attempt = 0;

        while ((player == null || playerT == null) && Time.time - t0 < timeout)
        {
            attempt++;

            var activeFpc = FindObjectOfType<FirstPersonController>();
            var allFpcs = Resources.FindObjectsOfTypeAll<FirstPersonController>();
            int allCount = allFpcs != null ? allFpcs.Length : 0;

            LogDbg($"[Scan {attempt}] active={(activeFpc ? activeFpc.name : "null")}, allCount={allCount}, t={Time.time - t0:0.00}s");

            if (allCount > 0)
            {
                for (int i = 0; i < allFpcs.Length; i++)
                {
                    var f = allFpcs[i]; if (!f) continue;
                    var go = f.gameObject;
                    LogDbg($"  • FPC[{i}] name={go.name}, activeInHierarchy={go.activeInHierarchy}, activeSelf={go.activeSelf}, scene={go.scene.name}");
                }
            }

            if (activeFpc != null)
            {
                player = activeFpc;
                playerT = player.transform;
                prevPlayerPos = playerT.position;
                LogDbg($"[Scan {attempt}] SUCCESS: set player={player.name} (scene={player.gameObject.scene.name})");
                yield break;
            }

            yield return null; // 다음 프레임 재시도
        }

        if (player == null || playerT == null)
            WarnDbg($"FAILED: No active FirstPersonController found within {timeout:0.00}s after load.");
    }
}
