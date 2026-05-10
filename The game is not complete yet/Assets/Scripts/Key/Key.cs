using UnityEngine;

/// <summary>
/// Represents a collectible key that supports object pooling and interacts with game objects in the scene.
/// </summary>
/// <remarks>Handles visual animation, pooling integration, and collection logic when interacting with specified
/// objects.</remarks>
public class Key : MonoBehaviour, IPoolable
{
    [SerializeField, Tooltip("Tags of the objects that can collect this key")]
    protected string[] tagsToCheck;
    [SerializeField, Tooltip("Key used for object pooling")]
    protected string poolKey;
    [SerializeField, Tooltip("Object pooler that owns this key")]
    protected ObjectPooler poolOwner;

    [Header("Animation Settings")]
    [SerializeField, Tooltip("Visual representation of the key")]
    private Transform visual;
    [SerializeField, Tooltip("Rotation speed of the key")]
    private float rotationSpeed = 50f;
    [SerializeField, Tooltip("Amplitude of the bobbing effect")]
    private float bobbingAmplitude = 0.25f;
    [SerializeField, Tooltip("Frequency of the bobbing effect")]
    private float bobbingFrequency = 1f;
    private Vector3 initialPosition;
    protected bool isCollected = false;

    // Properties for IPoolable interface
    public string PoolKey { get => poolKey; set => poolKey = value; }
    public ObjectPooler PoolOwner { get => poolOwner; set => poolOwner = value; }

    private void Update()
    {
        // Rotate the key
        visual.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime, Space.Self);

        // Bobbing effect
        float newY = Mathf.Sin(Time.time * bobbingFrequency) * bobbingAmplitude;
        Vector3 localPos = visual.localPosition;
        localPos.y = newY;
        visual.localPosition = localPos;
    }

    public virtual void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        foreach (string tagToCheck in tagsToCheck)
        {
            if (other.CompareTag(tagToCheck))
            {
                isCollected = true;
                GameManager.Instance.AddKey();

                AudioManager.Instance?.Play3DSFX("KeyPickup", transform.position);
                NarratorController.Instance?.PlayLine("Level1_3");
                poolOwner.ReturnToPool(gameObject, poolKey);

                break;
            }
        }
    }

    public void OnCreatedPool() { }
    public void OnReturnToPool() { }
    public void OnSpawnFromPool() { }
}