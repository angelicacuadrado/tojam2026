using UnityEngine;

public class Key : MonoBehaviour, IPoolable
{
    [SerializeField, Tooltip("Tag of the object that can collect this key")]
    private string tagToCheck;
    [SerializeField, Tooltip("Key used for object pooling")]
    private string poolKey;
    [SerializeField, Tooltip("Object pooler that owns this key")]
    private ObjectPooler poolOwner;

    [Header("Animation Settings")]
    [SerializeField, Tooltip("Visual representation of the key")]
    private Transform visual;
    [SerializeField, Tooltip("Rotation speed of the key")]
    private float rotationSpeed = 50f;
    [SerializeField, Tooltip("Amplitude of the bobbing effect")]
    private float bobbingAmplitude = 0.25f;
    [SerializeField, Tooltip("Frequency of the bobbing effect")]
    private float bobbingFrequency = 1f;
    [SerializeField, Tooltip("Initial position of the key for bobbing effect")]
    private Vector3 initialPosition;

    // Properties for IPoolable interface
    public string PoolKey { get => poolKey; set => poolKey = value; }
    public ObjectPooler PoolOwner { get => poolOwner; set => poolOwner = value; }

    private void Start()
    {
        initialPosition = transform.position;
    }

    private void Update()
    {
        // Rotate the key
        visual.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);

        // Bobbing effect
        float newY = initialPosition.y + Mathf.Sin(Time.time * bobbingFrequency) * bobbingAmplitude;
        Vector3 localPos = visual.localPosition;
        localPos.y = newY;
        visual.localPosition = localPos;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(tagToCheck))
        {
            GameManager.Instance.AddKey();
            poolOwner.ReturnToPool(gameObject, poolKey);
        }
    }

    public void OnCreatedPool() { }
    public void OnReturnToPool() { }
    public void OnSpawnFromPool() { }
}