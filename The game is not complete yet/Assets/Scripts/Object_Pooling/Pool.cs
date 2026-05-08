using UnityEngine;
/// <summary>
/// Represents a configuration for a pool of reusable GameObject instances.
/// </summary>
/// <remarks>Use this class to define the properties of an object pool, including the unique key, the prefab to
/// instantiate, the initial pool size, and whether the pool can grow beyond its initial size.</remarks>
[System.Serializable]
public class Pool
{
    [SerializeField, Tooltip("Unique key for this pool")]
    private string key;
    [SerializeField, Tooltip("Prefab to be pooled")]
    private GameObject prefab;
    [SerializeField, Tooltip("Initial size of the pool")]
    private int initialSize = 10;
    [SerializeField, Tooltip("Can the pool expand if needed")]
    private bool isExpandable = true;

    //Getters
    public string Key => key;
    public GameObject Prefab => prefab;
    public int InitialSize => initialSize;
    public bool IsExpandable => isExpandable;
}