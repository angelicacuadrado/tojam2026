using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private Transform firePoint;
    [SerializeField] private string bulletPoolKey = "Bullet";
    [SerializeField] private ObjectPooler bulletPool;
    [SerializeField] private float attackCooldown = 0.5f;
    private float lastAttackTime;
    private bool attackInput;

    private void Update()
    {
        if (attackInput && Time.time >= lastAttackTime + attackCooldown)
        {
            Attack();
            lastAttackTime = Time.time;
        }
    }
    public void OnAttack(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            attackInput = true;
    }

    private void Attack()
    {
        // Get a bullet from the pool
        GameObject bulletObj = bulletPool.Spawn(bulletPoolKey, firePoint.position, firePoint.rotation);
        if (bulletObj != null)
        {
            Bullet bullet = bulletObj.GetComponent<Bullet>();
            if (bullet != null)
            {
                // Initialize the bullet's direction based on the player's facing direction
                bullet.Initialize(firePoint.forward, bulletPool);
            }
        }
        attackInput = false; // Reset attack input after attacking
    }
}