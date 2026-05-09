using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EnemySpawnEntry
{
    public GameObject enemyPrefab;
    public int count = 1;
}

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform player;
    [SerializeField] private Transform pos1;
    [SerializeField] private Transform pos2;

    [Header("Spawn")]
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private float spawnSpacing = 1f;
    [SerializeField] private List<EnemySpawnEntry> enemiesToSpawn = new();

    private void Start()
    {
        if (spawnOnStart)
            SpawnAll();
    }

    public void SpawnAll()
    {
        if (enemiesToSpawn == null || enemiesToSpawn.Count == 0)
        {
            Debug.LogWarning($"{nameof(EnemySpawner)} on {name} has no enemies configured.", this);
            return;
        }

        foreach (EnemySpawnEntry entry in enemiesToSpawn)
        {
            SpawnEntry(entry);
        }
    }

    private void SpawnEntry(EnemySpawnEntry entry)
    {
        if (entry == null || entry.enemyPrefab == null)
        {
            Debug.LogWarning($"{nameof(EnemySpawner)} on {name} has an empty enemy spawn entry.", this);
            return;
        }

        int count = Mathf.Max(0, entry.count);
        for (int i = 0; i < count; i++)
        {
            Vector3 position = GetSpawnPosition(i, count);
            Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

            GameObject enemy = Instantiate(entry.enemyPrefab, position, rotation);
            ConfigureEnemy(enemy);
        }
    }

    private Vector3 GetSpawnPosition(int index, int count)
    {
        Vector3 center = spawnPoint != null ? spawnPoint.position : transform.position;

        if (count <= 1 || spawnSpacing <= 0f)
            return center;

        float angle = index * Mathf.PI * 2f / count;
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spawnSpacing;
        return center + offset;
    }

    private void ConfigureEnemy(GameObject enemy)
    {
        EnemyAttacker attacker = enemy.GetComponentInChildren<EnemyAttacker>();
        if (attacker != null)
            attacker.Configure(player, pos1, pos2);
    }
}
