/// <summary>
/// Defines methods that allow an object to participate in an object pool lifecycle.
/// </summary>
/// <remarks>Implement this interface to receive notifications when an object is created, retrieved from the pool,
/// or returned to the pool. This enables custom initialization, cleanup, or state management for pooled
/// objects.</remarks>
public interface IPoolable
{
    //The key identifying the pool this object belongs to
    public string PoolKey { get; set; }

    public ObjectPooler PoolOwner { get; set; }

    //Called once when the object is first created
    public void OnCreatedPool();

    //Called whenever the pool hands out this instance
    public void OnSpawnFromPool();

    //Called when returning to pool
    public void OnReturnToPool();
}
