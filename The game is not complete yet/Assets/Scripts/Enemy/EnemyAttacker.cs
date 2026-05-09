using UnityEngine;
using UnityEngine.AI;

public partial class EnemyAttacker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private int damagePerAttack = 1;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float knockbackForce = 6f;
    [SerializeField] private float knockbackUpwardForce = 0f;

    private NavMeshAgent agent;
    private PlayerHealth playerHealth;
    private EnemyState state = EnemyState.Chase;
    private float nextAttackTime;
    private Animator animator => GetComponentInChildren<Animator>();

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
            Debug.LogWarning($"{nameof(EnemyAttacker)} on {name} is missing a NavMeshAgent component.", this);
    }

    private void Update()
    {
        float playerDistance = Vector3.Distance(transform.position, player.position);

        if (playerDistance <= attackRange) { state = EnemyState.Attack; }
        else { state = EnemyState.Chase; }

        switch (state)
        {
            case EnemyState.Chase:
                HandleChase();
                break;

            case EnemyState.Attack:
                HandleAttack();
                break;
        }
    }

    public void Configure(Transform assignedPlayer)
    {
        player = assignedPlayer;
        if (player == null)
        {
            Debug.LogWarning($"{nameof(EnemyAttacker)} on {name} has no assigned player reference.", this);
            return;
        }
        playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogWarning($"{nameof(EnemyAttacker)} on {name} cannot find PlayerHealth component on assigned player.", this);
            return;
        }
    }

    private void HandleChase()
    {
        if (!CanUseAgent()) return;

        agent.isStopped = false;
        agent.SetDestination(player.position);
    }

    private void HandleAttack()
    {
        if (CanUseAgent()) agent.isStopped = true;

        if (Time.time < nextAttackTime || player.gameObject == null)
            return;

        Vector3 knockback = BuildKnockbackVector(player.position);
        playerHealth.TakeDamage(damagePerAttack, knockback);
        nextAttackTime = Time.time + attackCooldown;

        if (animator != null)
        {
            if (animator.name == "Zombie Cartoon_01")
            {
                animator.SetTrigger("attack");
            }
            else if (animator.name == "Skeleton_110")
            {
                switch (Random.Range(0f, 1f))
                {
                    case float n when (n < 0.5f):
                        animator.SetTrigger("attack1");
                        break;
                    default:
                        animator.SetTrigger("attack2");
                        break;
                }
            }

        }
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