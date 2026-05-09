using UnityEngine;

public class Bullet : MonoBehaviour, IPoolable
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private string poolKey = "Bullet";
    [SerializeField] private ObjectPooler poolOwner;
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifetime = 5f;
    private Vector3 direction;

    // Properties
    public string PoolKey { get => poolKey; set => poolKey = value; }
    public ObjectPooler PoolOwner { get => poolOwner; set => poolOwner = value; }

    public void Initialize(Vector3 direction, ObjectPooler owner)
    {
        this.direction = direction.normalized;
        this.poolOwner = owner;
        Invoke(nameof(ReturnToPool), lifetime); // Automatically return to pool after lifetime expires
    }

    private void Update()
    {
        transform.position += speed * Time.deltaTime * direction;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the collider has an IAttackable component and apply damage
        IAttackable attackable = other.GetComponent<IAttackable>();
        // Check parent if not found on the object itself
        attackable ??= other.GetComponentInParent<IAttackable>();
        // Apply damage 
        attackable?.TakeDamage(damage);

        // Return the bullet to the pool
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (poolOwner != null)
        {
            poolOwner.ReturnToPool(gameObject, poolKey);
        }
        else
        {
            Destroy(gameObject); // Fallback if no pool owner is set
        }
    }

    public void OnCreatedPool() { }
    public void OnSpawnFromPool() { }
    public void OnReturnToPool() { }
}
