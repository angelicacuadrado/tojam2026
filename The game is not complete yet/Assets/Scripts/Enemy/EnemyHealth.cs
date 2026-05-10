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
    private Animator animator => GetComponentInChildren<Animator>();

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

        animator?.SetTrigger("hit");

        if (currentHP == 0)
        {
            HandleZeroHP();
        }
        else
        {
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
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.Play3DSFX("EnemyDie", transform.position);
        }

        if (animator == null)
            return;

        animator.applyRootMotion = true;
        animator.SetTrigger("die");
    }

    private void Respawn()
    {
        if (!TryConsumeRespawn())
        {
            Die();
            return;
        }

        lastDeathPosition = transform.position;
        ResetHealth();
        MoveToRespawnPosition();

        animator?.SetTrigger("rebrith");
    }

    private void RespawnAndGrow()
    {
        if (!TryConsumeRespawn())
        {
            Die();
            return;
        }

        lastDeathPosition = transform.position;
        ResetHealth();
        MoveToRespawnPosition();

        EnemyGrower grower = GetComponent<EnemyGrower>();
        if (grower != null)
            grower.Grow(growthScaleMultiplier);
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

    public void OnDeathAnimationComplete()
    {
        if (useDestroyOnDie)
            Destroy(gameObject);

        gameObject.SetActive(false);
    }
}