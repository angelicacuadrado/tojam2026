using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyDamageTester : MonoBehaviour
{
    [SerializeField] private EnemyHealth enemyHealth;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame && enemyHealth != null)
        {
            enemyHealth.TakeDamage(1);
        }
    }
}
