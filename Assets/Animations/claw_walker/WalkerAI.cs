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


    [Header("Search Visuals")]
    [SerializeField] private Color searchFromColor = Color.white; // Patrol 시 색 (복귀 색)
    [SerializeField] private Color searchColor = Color.red;    // Search/Rage 시 색
    [SerializeField] private float searchColorFade = 0.4f;        // 색 전환 시간(초)


    [Header("Player Catch (Game Over)")]
    [SerializeField] private float gameOverDistance = 0.5f; // Rage 중 이 거리 이하면 GameOver




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

        // 애니메이터 Speed가 0(Idle) 또는 4(Run)일 땐 비활성화
        float animSpeed = animator ? animator.GetFloat(speedParam) : 0f;
        const float EPS = 0.05f;
        bool disableYaw = Mathf.Abs(animSpeed - 0f) < EPS || Mathf.Abs(animSpeed - 4f) < EPS;

        if (disableYaw)
        {
            // 본체 회전으로 복귀
            visualRoot.rotation = Quaternion.Slerp(
                visualRoot.rotation,
                transform.rotation,
                Mathf.Clamp01(Time.deltaTime * yawLerpSpeed)
            );
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

        // ─ Rage: 기존 유지/추적/해제
        if (state == WalkerState.Rage)
        {
            // (1) 플레이어 움직임 누적
            if ((playerT.position - prevPlayerPos).magnitude > playerMoveThresh)
                playerMoveTimer += Time.deltaTime;
            else
                playerMoveTimer = 0f;
            prevPlayerPos = playerT.position;

            // (2) 발소리 들리면 마지막 위치/시간 갱신
            if (heardFootstepNow)
            {
                lastHeardPlayerPos = playerT.position;
                lastFootstepHeardTime = Time.time;
            }

            // (3) 추적 개시
            if (playerMoveTimer >= playerMoveSecondsToChase && Time.time >= nextChaseAllowedTime)
            {
                SnapRunTo(lastHeardPlayerPos);
                nextChaseAllowedTime = Time.time + requeryFootstepInterval;
            }

            // (4) 재평가
            if (Time.time >= nextChaseAllowedTime && Time.time - lastFootstepHeardTime <= requeryFootstepInterval)
            {
                SnapRunTo(lastHeardPlayerPos);
                nextChaseAllowedTime = Time.time + requeryFootstepInterval;
            }

            // (5) 추가 발소리 없으면 Rage 해제
            if (Time.time - lastFootstepHeardTime > rageForgetAfter)
            {
                ExitRage();
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

        // 빛 고정: maxLightRange - 3
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

        StopBreathing();
        PlayScream();

        // 초기화
        lastFootstepHeardTime = Time.time;
        nextChaseAllowedTime = Time.time;
        if (playerT) { lastHeardPlayerPos = playerT.position; prevPlayerPos = playerT.position; }
        playerMoveTimer = 0f;

    }

    private void ExitRage()
    {
        state = WalkerState.Patrol;

        StopScream();
        PlayBreathing();

        if (pointLight)
        {
            if (lightCoroutine != null) StopCoroutine(lightCoroutine);
            // range/intensity는 기존 로직 유지
        }
        if (pointLight2)
        {
            if (lightCoroutine2 != null) StopCoroutine(lightCoroutine2);
        }

        // 순찰 재개
        if (agent != null)
        {
            agent.isStopped = false;
            agent.speed = walkSpeed;
        }
        GoToNearestWaypointReachable();

        // ✨ Patrol 색으로 전환
        StartLightColorFade(searchFromColor);
        if (player != null) player.EndThreatTint(searchColorFade);   // 플레이어 라이트도 서서히 흰색 복귀

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
        // 참조가 비어 있으면 즉시 재획득 시도
        if (player == null || playerT == null)
        {
            player = FindObjectOfType<FirstPersonController>();
            if (player != null) playerT = player.transform;
            if (playerT == null) return;
        }

        // 수평(XZ) 거리만으로 판정 (y 높이 차이는 무시)
        Vector3 a = transform.position;
        Vector3 b = playerT.position;
        float distXZ = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        float dy = Mathf.Abs(a.y - b.y); // 참고용 출력만

        if (distXZ <= gameOverDistance)
        {
            Debug.Log($"[WalkerAI] GAME OVER TRIGGER (Rage): distXZ={distXZ:F3} <= {gameOverDistance:F3} (Δy={dy:F3})");
            player.TriggerGameOver("CaughtByWalker");
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

}
