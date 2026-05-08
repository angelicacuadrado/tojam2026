using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
/// <summary>
/// Manages object pooling for multiple prefab types, enabling efficient reuse of GameObjects.
/// </summary>
/// <remarks>ObjectPooler maintains pools of inactive GameObjects, allowing objects to be spawned and returned
/// without incurring the overhead of frequent instantiation and destruction. Pools are defined via the inspector, and
/// each pool can be configured to expand if needed.</remarks>
public class ObjectPooler : MonoBehaviour
{
    [SerializeField, Tooltip("The parent transform for pooled objects")]
    private Transform poolRoot;
    [SerializeField, Tooltip("List of pool definitions, add a new pool for each type of object you want to pool")]
    private List<Pool> pools = new();

    private Dictionary<string, Queue<GameObject>> poolDictionary;

    /// <summary>
    /// Initializes the object pool system and prepares all defined pools for use.
    /// </summary>
    /// <remarks>This method is called automatically by Unity when the script instance is being loaded. It
    /// sets up the internal data structures required for pooling and instantiates the initial set of pooled objects for
    /// each configured pool. This ensures that objects are ready to be retrieved from the pool as soon as the game
    /// starts.</remarks>
    private void Awake()
    {
        //Initialize the pool dictionary
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        //Create each pool
        foreach (var pool in pools)
        {
            var queue = new Queue<GameObject>();

            for (int i = 0; i < pool.InitialSize; i++)
            {
                //Instantiate the object and set it inactive
                var obj = Instantiate(pool.Prefab, poolRoot);
                obj.SetActive(false);

                // Notify the object it has been created in the pool
                if (obj.TryGetComponent<IPoolable>(out var poolable))
                {
                    // Assign pool key and owner before calling OnCreatedPool
                    poolable.PoolKey = pool.Key;
                    poolable.PoolOwner = this;
                    poolable.OnCreatedPool();
                }

                //Enqueue the object
                queue.Enqueue(obj);
            }
            //Add the queue to the dictionary
            poolDictionary.Add(pool.Key, queue);
        }
    }

    /// <summary>
    /// Spawns an object from the specified pool at the given position and rotation.
    /// </summary>
    /// <remarks>If the pool is empty and not expandable, or if the specified key does not correspond to an
    /// existing pool, the method returns null. The spawned object is activated and its transform is set to the
    /// specified position and rotation. If the object implements IPoolable, its OnSpawnFromPool method is 
    /// called upon spawning and it is given the pool's key.</remarks>
    /// <param name="key">The key identifying the object pool from which to spawn the object. Must correspond to an existing pool.</param>
    /// <param name="position">The world position at which to spawn the object.</param>
    /// <param name="rotation">The rotation to apply to the spawned object.</param>
    /// <returns>The spawned GameObject if successful; otherwise, null if the pool does not exist or cannot be expanded.</returns>
    public GameObject Spawn(string key, Vector3 position, Quaternion rotation)
    {
        //Check if the pool exists
        if (!poolDictionary.TryGetValue(key, out var queue))
        {
            Debug.LogWarning($"Pool with key '{key}' does not exist.");
            return null;
        }

        //Check if we need to expand the pool
        if (queue.Count == 0)
        {
            var def = pools.Find(p => p.Key == key);
            //Expand the pool if allowed
            if (def != null && def.IsExpandable)
            {
                var extra = Instantiate(def.Prefab, poolRoot);
                extra.SetActive(false);

                if (extra.TryGetComponent<IPoolable>(out var p))
                {
                    p.PoolKey = def.Key;
                    p.PoolOwner = this;
                }

                queue.Enqueue(extra);
            }
            //If not expandable, return null
            else
            {
                Debug.LogWarning($"Pool '{key}' is empty and not expandable.");
                return null;
            }
        }

        //Dequeue the next object
        var obj = queue.Dequeue();

        //Set position and activate
        obj.transform.SetParent(null);
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);

        // Notify the object it has been spawned from the pool
        if (obj.TryGetComponent<IPoolable>(out var poolable))
        {
            poolable.PoolKey = key;
            poolable.PoolOwner = this;
            poolable.OnSpawnFromPool();
        }

        return obj;
    }

    /// <summary>
    /// Returns the specified GameObject to the object pool, deactivating it and resetting its parent transform.
    /// </summary>
    /// <remarks>If the GameObject implements the IPoolable interface, its OnReturnToPool method is called
    /// before deactivation. The object is deactivated and reparented to the pool's root transform. This method is
    /// typically used to recycle objects for reuse and improve performance by minimizing instantiation and
    /// destruction.</remarks>
    /// <param name="obj">The GameObject to return to the pool. If null, the method does nothing.</param>
    public void ReturnToPool(GameObject obj, string key)
    {
        if (obj == null) return;

        //Notify the object it is being returned to the pool
        obj.GetComponent<IPoolable>()?.OnReturnToPool();

        //Deactivate and reparent
        obj.SetActive(false);
        obj.transform.SetParent(poolRoot);

        if (poolDictionary.TryGetValue(key, out var queue)) { queue.Enqueue(obj); }
        else { Debug.LogWarning($"Trying to return object to unknown pool '{key}'. Destroying instead."); Destroy(obj); }
    }
}