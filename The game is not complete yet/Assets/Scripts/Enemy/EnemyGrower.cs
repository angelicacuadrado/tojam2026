using UnityEngine;
using UnityEngine.AI;

public class EnemyGrower : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] private float patrolPointReachDistance = 0.3f;
    [SerializeField] private Transform[] patrolPoints;
    private int currentPatrolIndex = 0;
    private EnemyState state = EnemyState.Patrol;
    private NavMeshAgent agent;


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
                AudioManager.Instance.Play3DSFX("EnemyGrow", transform.position);
            }
            if (NarratorController.Instance != null)
            {
                NarratorController.Instance.PlayLine("Level2_2");
            }
        }
    }

    private void HandlePatrol()
    {
        if (!CanUseAgent())
            return;

        agent.isStopped = false;

        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        // Check if we've reached the patrol point (considering only horizontal distance).
        Vector3 patrolPoint = new Vector3(patrolPoints[currentPatrolIndex].position.x,
            transform.position.y, patrolPoints[currentPatrolIndex].position.z);
        // If we're within the reach distance of the patrol point, move to the next one.
        if (Vector3.Distance(transform.position, patrolPoint) <= patrolPointReachDistance)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }
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
            Debug.LogWarning($"{nameof(EnemyAttacker)} on {name} is not on a NavMesh.", this);
        }
        return false;
    }
}
