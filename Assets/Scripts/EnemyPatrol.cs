using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyPatrol : MonoBehaviour
{
    public Transform[] waypoints;
    private int currentWaypointIndex = 0;
    private NavMeshAgent agent;
    private Animator animator;

    public float turnThreshold = 15f;
    public float turnStopThreshold = 3f; // 회전 종료 기준 각도
    public float turnSpeed = 80f; // 초당 회전 속도

    private bool isTurning = false;
    private Vector3 desiredDirection;

    private float curX;
    private float curZ;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        MoveToNextWaypoint();
    }

    void Update()
    {
        if (isTurning)
        {
            HandleTurning();
            return;
        }

        if (agent.pathPending) return;

        if (agent.remainingDistance < 0.5f)
        {
            Vector3 targetDir = (waypoints[currentWaypointIndex].position - transform.position).normalized;
            float angle = Vector3.SignedAngle(transform.forward, targetDir, Vector3.up);

            if (Mathf.Abs(angle) >= turnThreshold)
            {
                desiredDirection = targetDir;
                StartTurning(angle);
            }
            else
            {
                MoveToNextWaypoint();
            }
        }
    }

    void StartTurning(float angle)
    {
        curX = agent.transform.position.x;
        curZ = agent.transform.position.z; 
        isTurning = true;
        agent.isStopped = true;

        if (angle > 30)
        {
            animator.SetTrigger("TurnRight");
        }
        else
        {
            animator.SetTrigger("TurnLeft");
        }
    }

    void HandleTurning()
    {
        // 실제 회전 수행
        Quaternion targetRotation = Quaternion.LookRotation(desiredDirection, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

        float angleToTarget = Vector3.Angle(transform.forward, desiredDirection);

        if (angleToTarget <= turnStopThreshold)
        {
            StopTurning();
        }
    }

    void StopTurning()
    {
        animator.SetTrigger("StopTurn");

        isTurning = false;
        agent.isStopped = false;

        MoveToNextWaypoint();
    }


    void MoveToNextWaypoint()
    {
        if (waypoints.Length == 0) return;

        agent.SetDestination(waypoints[currentWaypointIndex].position);
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    }
}
