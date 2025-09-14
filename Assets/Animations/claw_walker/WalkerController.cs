using System.Collections;           // ← 이 줄 추가
using UnityEngine;
using UnityEngine.AI; 

public class WalkerController : MonoBehaviour
{
    public enum WalkerState { Idle, Patrol, Chase }

    [Header("AI Settings")]
    public WalkerState currentState = WalkerState.Idle;
    public Transform[] waypoints;
    public float waypointThreshold = 1f;
    private int currentWaypointIndex = 0;

    [Header("Detection Settings")]
    public Light playerLight;   // 플레이어의 PointLight
    public float detectionRange = 8f;

    [Header("Components")]
    private NavMeshAgent agent;
    private Animator animator;

    [SerializeField] private Light footstepLight;
    [SerializeField] private float pulseRange = 5f;
    [SerializeField] private float pulseDuration = 0.5f;
    private Coroutine pulseCoroutine;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (waypoints.Length > 0)
        {
            currentState = WalkerState.Patrol;
            MoveToNextWaypoint();
        }
    }

    private void Update()
    {
        switch (currentState)
        {
            case WalkerState.Idle:
                animator.SetBool("isWalking", false);
                animator.SetBool("isRunning", false);
                break;

            case WalkerState.Patrol:
                HandlePatrol();
                DetectPlayerLight();
                break;

            case WalkerState.Chase:
                HandleChase();
                break;
        }
    }

    private void HandlePatrol()
    {
        animator.SetBool("isWalking", true);
        animator.SetBool("isRunning", false);

        if (!agent.pathPending && agent.remainingDistance < waypointThreshold)
        {
            MoveToNextWaypoint();
        }
    }

    private void MoveToNextWaypoint()
    {
        if (waypoints.Length == 0) return;

        agent.destination = waypoints[currentWaypointIndex].position;
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    }

    private void DetectPlayerLight()
    {
        if (playerLight != null && playerLight.range > 0f)
        {
            float distance = Vector3.Distance(transform.position, playerLight.transform.position);
            if (distance <= playerLight.range + detectionRange)
            {
                currentState = WalkerState.Chase;
                agent.destination = playerLight.transform.position; // 빛의 중심 좌표
            }
        }
    }

    private void HandleChase()
    {
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", true);

        if (playerLight != null)
        {
            agent.destination = playerLight.transform.position;
        }
    }

    public void OnFootstep()
    {
        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
        pulseCoroutine = StartCoroutine(PulseLight());
    }

    private IEnumerator PulseLight()
    {
        float timer = 0f;
        float halfDuration = pulseDuration / 2f;

        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            footstepLight.range = Mathf.Lerp(0, pulseRange, timer / halfDuration);
            yield return null;
        }

        timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            footstepLight.range = Mathf.Lerp(pulseRange, 0, timer / halfDuration);
            yield return null;
        }

        footstepLight.range = 0f;
        yield return null;
    }
    
}
