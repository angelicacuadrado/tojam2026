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
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("PlayerShoot",0.2f);
        }
        else
        {
            Debug.LogWarning("AudioManager not found for shooting.");
        }

        GameObject bulletObj = bulletPool.Spawn(bulletPoolKey, firePoint.position, firePoint.rotation);
        if (bulletObj != null)
        {
            Bullet bullet = bulletObj.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.Initialize(firePoint.forward, bulletPool);
            }
        }

        attackInput = false;
    }
}