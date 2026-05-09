using UnityEngine;
using UnityEngine.AI;

public enum EnemyZeroHPBehavior
{
    Die,
    Respawn,
    RespawnAndGrow
}

public class EnemyHealth : MonoBehaviour
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
            HandleZeroHP();
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
        if (useDestroyOnDie)
        {
            Destroy(gameObject);
            return;
        }

        gameObject.SetActive(false);
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
}
