using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemySpawnTrigger : MonoBehaviour
{
    [SerializeField] private string requiredTag = "Player";
    [SerializeField] private bool spawnOnlyOnce = true;
    [SerializeField] private EnemySpawner[] spawners;

    private bool hasSpawned;

    private void Reset()
    {
        if (TryGetComponent(out Collider triggerCollider))
            triggerCollider.isTrigger = true;
    }

    private void Awake()
    {
        if (TryGetComponent(out Collider triggerCollider) && !triggerCollider.isTrigger)
            Debug.LogWarning($"{nameof(EnemySpawnTrigger)} on {name} needs its Collider set to Is Trigger.", this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (spawnOnlyOnce && hasSpawned)
            return;

        if (!string.IsNullOrWhiteSpace(requiredTag) && !other.CompareTag(requiredTag))
            return;

        if (NarratorController.Instance != null)
        {
            NarratorController.Instance.PlayLine("Level3_4");
        }

        SpawnEnemies();
    }

    public void SpawnEnemies()
    {
        if (spawnOnlyOnce && hasSpawned)
            return;

        hasSpawned = true;

        if (spawners == null || spawners.Length == 0)
        {
            Debug.LogWarning($"{nameof(EnemySpawnTrigger)} on {name} has no spawners assigned.", this);
            return;
        }

        foreach (EnemySpawner spawner in spawners)
        {
            if (spawner != null)
                spawner.SpawnAll();
        }
    }
}
