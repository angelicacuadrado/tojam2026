using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour, IAttackable
{
    [Header("Health")]
    [SerializeField] private int maxHP = 3;
    [SerializeField] private int currentHP;

    [Header("Zero HP")]
    [SerializeField] private EnemyZeroHPBehavior zeroHPBehavior = EnemyZeroHPBehavior.Die;
    [SerializeField] private int respawnCount = 0;
    [SerializeField] private float growthScaleMultiplier = 1.2f;
    [SerializeField] private bool useDestroyOnDie = true;

    private Vector3 lastDeathPosition;
    private int respawnsRemaining;
    private bool pendingRespawn;
    private bool hasStoredAgentSpeed;
    private float storedAgentSpeed;
    private Animator animator => GetComponent<Animator>();

    [SerializeField] private GameObject keyPrefab;

    //Properties
    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;

    private void Awake()
    {
        respawnsRemaining = respawnCount;
        ResetHealth();
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || currentHP <= 0)
            return;

        currentHP = Mathf.Max(0, currentHP - amount);

        if (currentHP == 0)
        {
            HandleZeroHP();
        }
        else
        {
            animator?.SetTrigger("hit");

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.Play3DSFX("EnemyDamage", transform.position);
            }
        }
    }

    public void ResetHealth()
    {
        currentHP = Mathf.Max(1, maxHP);
    }

    private void HandleZeroHP()
    {
        switch (zeroHPBehavior)
        {
            case EnemyZeroHPBehavior.Die:
                Die();
                break;

            case EnemyZeroHPBehavior.Respawn:
                Respawn();
                break;

            case EnemyZeroHPBehavior.RespawnAndGrow:
                RespawnAndGrow();
                break;
        }
    }

    private void Die()
    {
        pendingRespawn = false;
        StopAgentForDeath();
        PlayDeathAnimation();
    }

    private void Respawn()
    {
        if (!TryConsumeRespawn())
        {
            Die();
            return;
        }

        pendingRespawn = true;
        StopAgentForDeath();
        PlayDeathAnimation();
    }

    private void RespawnAndGrow()
    {
        if (!TryConsumeRespawn())
        {
            Die();
            return;
        }

        pendingRespawn = false;
        lastDeathPosition = transform.position;
        ResetHealth();
        MoveToRespawnPosition();

        EnemyGrower grower = GetComponent<EnemyGrower>();
        if (grower != null)
        {
            grower.Grow(growthScaleMultiplier);
        }

        RestoreAgentMovement();
    }

    private bool TryConsumeRespawn()
    {
        if (respawnCount < 0)
            return true;

        if (respawnsRemaining <= 0)
            return false;

        respawnsRemaining--;
        return true;
    }

    private void MoveToRespawnPosition()
    {
        if (TryGetComponent(out NavMeshAgent agent) && agent.enabled && agent.isOnNavMesh)
        {
            agent.Warp(lastDeathPosition);
            return;
        }

        transform.position = lastDeathPosition;
    }

    private void PlayDeathAnimation()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.Play3DSFX("EnemyDie", transform.position);
        }

        if (animator == null)
        {
            OnDeathAnimationComplete();
            return;
        }

        animator.applyRootMotion = true;
        animator.ResetTrigger("hit");
        animator.SetTrigger("die");
    }

    private void CompleteRespawn()
    {
        lastDeathPosition = transform.position;
        ResetHealth();
        MoveToRespawnPosition();

        pendingRespawn = false;

        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.SetTrigger("rebrith");
        }
        else
        {
            OnRespawnAnimationComplete();
        }
    }

    private void StopAgentForDeath()
    {
        if (!TryGetComponent(out NavMeshAgent agent))
        {
            return;
        }

        if (!hasStoredAgentSpeed)
        {
            storedAgentSpeed = agent.speed;
            hasStoredAgentSpeed = true;
        }

        agent.speed = 0f;
        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.velocity = Vector3.zero;
            agent.isStopped = true;
        }
    }

    public void OnRespawnAnimationComplete()
    {
        RestoreAgentMovement();
    }

    private void RestoreAgentMovement()
    {
        if (!TryGetComponent(out NavMeshAgent agent))
        {
            return;
        }

        if (hasStoredAgentSpeed)
        {
            agent.speed = storedAgentSpeed;
        }

        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }

    public void OnDeathAnimationComplete()
    {
        if (pendingRespawn)
        {
            CompleteRespawn();
            return;
        }

        if (keyPrefab != null)
        {
            Instantiate(keyPrefab, transform.position, Quaternion.identity);
        }

        if (useDestroyOnDie)
        {
            Destroy(gameObject);
            return;
        }

        gameObject.SetActive(false);
    }
}
