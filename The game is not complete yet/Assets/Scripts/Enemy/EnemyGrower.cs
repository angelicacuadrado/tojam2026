using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyGrower : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;

    [SerializeField] private float patrolDelay;
    private int currentPatrolIndex = 0;
    private NavMeshAgent agent;
    private bool isGrown = false;
    private bool loggedMissingPatrolPoints;


    [Header("Growth")]
    [SerializeField] private float maxScaleMultiplier = 3f;
    private Vector3 initialScale;

    private void Awake()
    {
        initialScale = transform.localScale;
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
            Debug.LogWarning($"{nameof(EnemyGrower)} on {name} is missing a NavMeshAgent component.", this);
    }

    private void Update()
    {
        HandlePatrol();
    }

    public void Grow(float multiplier)
    {
        if (multiplier <= 0f)
            return;

        Vector3 targetScale = transform.localScale * multiplier;

        if (maxScaleMultiplier > 0f)
        {
            Vector3 maxScale = initialScale * maxScaleMultiplier;
            targetScale = new Vector3(
                Mathf.Min(targetScale.x, maxScale.x),
                Mathf.Min(targetScale.y, maxScale.y),
                Mathf.Min(targetScale.z, maxScale.z)
            );
        }

        if (transform.localScale != targetScale)
        {
            transform.localScale = targetScale;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("EnemyGrow");
            }
            if (NarratorController.Instance != null && !isGrown)
            {
                NarratorController.Instance.PlayLine("Level2_2");
                isGrown = true;
            }
        }
    }

    private void HandlePatrol()
    {
        if (!CanUseAgent())
            return;

        if (!HasPatrolPoints())
            return;

        agent.isStopped = false;
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsCurrentPatrolTrigger(other))
            return;

        StartCoroutine(PatrolDelay());

        
    }

    private IEnumerator PatrolDelay()
    {
        yield return new WaitForSeconds(patrolDelay);
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }

    private bool HasPatrolPoints()
    {
        if (patrolPoints != null && patrolPoints.Length > 0 && patrolPoints[currentPatrolIndex] != null)
            return true;

        if (!loggedMissingPatrolPoints)
        {
            Debug.LogWarning($"{nameof(EnemyGrower)} on {name} needs patrol point trigger zones.", this);
            loggedMissingPatrolPoints = true;
        }

        return false;
    }

    private bool IsCurrentPatrolTrigger(Collider other)
    {
        if (other == null || !HasPatrolPoints())
            return false;

        Transform currentPatrolPoint = patrolPoints[currentPatrolIndex];
        return other.transform == currentPatrolPoint || other.transform.IsChildOf(currentPatrolPoint);
    }

    private bool CanUseAgent()
    {
        // The agent must be enabled and on a NavMesh to be used for movement.
        if (agent == null || !agent.enabled)
            return false;

        // Check if the agent is on a NavMesh. If not, log a warning and return false.
        if (agent.isOnNavMesh)
            return true;

        else
        {
            Debug.LogWarning($"{nameof(EnemyGrower)} on {name} is not on a NavMesh.", this);
        }
        return false;
    }
}
