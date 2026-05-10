using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField, Tooltip("Maximum health of the player")]
    private int maxHealth = 3;
    [SerializeField, Tooltip("Current health of the player")]
    private int currentHealth;

    [Header("Invincibility Settings")]
    [SerializeField, Tooltip("Duration of invincibility after taking damage")]
    private float invincibilityDuration = 1f;
    [SerializeField, Tooltip("Timer to track invincibility")]
    private float invincibilityTimer = 0f;

    [Header("Knockback Settings")]
    private Rigidbody rb;
    [SerializeField, Tooltip("Force applied to the player when taking damage")]
    private float knockbackForce = 5f;

    [Header("Respawn Settings")]
    [SerializeField, Tooltip("Respawn point for the player")]
    private Transform respawnPoint;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    public event System.Action<int, int> HealthChanged;

    private void Start()
    {
        SetCurrentHealth(maxHealth);
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning("PlayerHealth: Rigidbody component not found on the GameObject. Please assign it in the inspector.");
        }
    }

    private void Update()
    {
        // Handle invincibility timer
        if (invincibilityTimer > 0)
        {
            invincibilityTimer -= Time.deltaTime;
        }
    }

    public void TakeDamage(int damage, Vector3 direction)
    {
        if (damage <= 0) return;
        if (invincibilityTimer > 0) return;

        SetCurrentHealth(currentHealth - damage);
        if (currentHealth <= 0)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("PlayerDie");
            }

            Invoke(nameof(Respawn), 2.0f);
        }
        else
        {
            // Apply knockback
            rb.AddForce(direction * knockbackForce, ForceMode.Impulse);
            // Start invincibility
            invincibilityTimer = invincibilityDuration;
            //Play damage sound effect
            AudioManager.Instance.PlaySFX("PlayerDamage");
        }
    }

    private void Respawn()
    {
        SetCurrentHealth(maxHealth);
        transform.position = respawnPoint.position;
        AudioManager.Instance.PlaySFX("PlayerRespawn");
        rb.linearVelocity = Vector3.zero; // Reset velocity on respawn
    }

    private void SetCurrentHealth(int value)
    {
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
