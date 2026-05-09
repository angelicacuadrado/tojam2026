using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAttacker : MonoBehaviour
{
    private enum EnemyState
    {
        Patrol,
        Chase,
        Attack
    }

    [Header("References")]
    [SerializeField] private Transform pos1;
    [SerializeField] private Transform pos2;
    [SerializeField] private Transform player;

    [Header("Detection")]
    [SerializeField] private bool canChasePlayer = true;
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float attackRange = 1.5f;

    [Header("Attack")]
    [SerializeField] private int damagePerAttack = 1;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float knockbackForce = 6f;
    [SerializeField] private float knockbackUpwardForce = 0f;

    [Header("Patrol")]
    [SerializeField] private float patrolPointReachDistance = 0.3f;

    private NavMeshAgent agent;
    private PlayerHealth playerHealth;
    private EnemyState state = EnemyState.Patrol;
    private Transform currentPatrolTarget;
    private float nextAttackTime;
    private bool warnedMissingPlayer;
    private bool warnedMissingPlayerHealth;
    private bool warnedAgentOffNavMesh;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        CachePlayerHealth();
        SetPatrolTarget(pos1 != null ? pos1 : pos2);
    }

    private void Update()
    {
        if (player == null)
        {
            WarnMissingPlayer();
            HandlePatrol();
            return;
        }

        CachePlayerHealth();

        if (!canChasePlayer)
        {
            state = EnemyState.Patrol;
        }
        else
        {
            float playerDistance = Vector3.Distance(transform.position, player.position);

            if (playerDistance <= attackRange)
                state = EnemyState.Attack;
            else if (playerDistance <= detectionRange)
                state = EnemyState.Chase;
            else
                state = EnemyState.Patrol;
        }

        switch (state)
        {
            case EnemyState.Patrol:
                HandlePatrol();
                break;

            case EnemyState.Chase:
                HandleChase();
                break;

            case EnemyState.Attack:
                HandleAttack();
                break;
        }
    }

    public void Configure(Transform assignedPlayer, Transform patrolPos1, Transform patrolPos2)
    {
        player = assignedPlayer;
        pos1 = patrolPos1;
        pos2 = patrolPos2;
        CachePlayerHealth();
        SetPatrolTarget(pos1 != null ? pos1 : pos2);
    }

    private void HandlePatrol()
    {
        if (!CanUseAgent())
            return;

        agent.isStopped = false;

        if (currentPatrolTarget == null)
            SetPatrolTarget(pos1 != null ? pos1 : pos2);

        if (currentPatrolTarget == null)
            return;

        agent.SetDestination(currentPatrolTarget.position);

        if (HasReachedPatrolTarget())
            SetPatrolTarget(currentPatrolTarget == pos1 ? pos2 : pos1);
    }

    private bool HasReachedPatrolTarget()
    {
        if (currentPatrolTarget == null)
            return false;

        Vector3 flatDelta = currentPatrolTarget.position - transform.position;
        flatDelta.y = 0f;
        if (flatDelta.sqrMagnitude <= patrolPointReachDistance * patrolPointReachDistance)
            return true;

        if (agent.pathPending)
            return false;

        float reachDistance = Mathf.Max(agent.stoppingDistance, patrolPointReachDistance);
        return agent.remainingDistance <= reachDistance;
    }

    private void HandleChase()
    {
        if (!CanUseAgent())
            return;

        agent.isStopped = false;
        agent.SetDestination(player.position);
    }

    private void HandleAttack()
    {
        if (CanUseAgent())
            agent.isStopped = true;

        TryAttackPlayer(player.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryAttackPlayer(collision.gameObject);
    }

    private void OnCollisionStay(Collision collision)
    {
        TryAttackPlayer(collision.gameObject);
    }

    private void TryAttackPlayer(GameObject targetObject)
    {
        if (Time.time < nextAttackTime || targetObject == null)
            return;

        PlayerHealth targetHealth = targetObject == player.gameObject && playerHealth != null
            ? playerHealth
            : FindPlayerHealth(targetObject);

        if (targetHealth == null)
        {
            if (targetObject.transform == player)
                WarnMissingPlayerHealth();

            return;
        }

        Vector3 knockback = BuildKnockbackVector(targetHealth.transform.position);
        targetHealth.TakeDamage(damagePerAttack, knockback);
        nextAttackTime = Time.time + attackCooldown;
    }

    private Vector3 BuildKnockbackVector(Vector3 targetPosition)
    {
        Vector3 knockbackDirection = targetPosition - transform.position;
        knockbackDirection.y = 0f;

        if (knockbackDirection.sqrMagnitude < 0.001f)
            knockbackDirection = transform.forward;

        knockbackDirection.Normalize();

        Vector3 knockback = knockbackDirection * knockbackForce;
        knockback += Vector3.up * knockbackUpwardForce;
        return knockback;
    }

    private void CachePlayerHealth()
    {
        if (player == null)
            return;

        playerHealth = FindPlayerHealth(player.gameObject);

        if (playerHealth == null)
            WarnMissingPlayerHealth();
    }

    private PlayerHealth FindPlayerHealth(GameObject targetObject)
    {
        PlayerHealth health = targetObject.GetComponentInParent<PlayerHealth>();
        if (health != null)
            return health;

        return targetObject.GetComponentInChildren<PlayerHealth>();
    }

    private void SetPatrolTarget(Transform target)
    {
        currentPatrolTarget = target;
    }

    private bool CanUseAgent()
    {
        if (agent == null || !agent.enabled)
            return false;

        if (agent.isOnNavMesh)
            return true;

        if (!warnedAgentOffNavMesh)
        {
            Debug.LogWarning($"{nameof(EnemyAttacker)} on {name} is not on a NavMesh.", this);
            warnedAgentOffNavMesh = true;
        }

        return false;
    }

    private void WarnMissingPlayer()
    {
        if (warnedMissingPlayer)
            return;

        Debug.LogWarning($"{nameof(EnemyAttacker)} on {name} has no player assigned.", this);
        warnedMissingPlayer = true;
    }

    private void WarnMissingPlayerHealth()
    {
        if (warnedMissingPlayerHealth)
            return;

        Debug.LogWarning($"{nameof(EnemyAttacker)} on {name} could not find PlayerHealth on the assigned player.", this);
        warnedMissingPlayerHealth = true;
    }
}
