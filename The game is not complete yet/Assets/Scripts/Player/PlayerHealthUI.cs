using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image[] hearts = new Image[3];

    private void Awake()
    {
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }
    }

    private void OnEnable()
    {
        if (playerHealth == null)
        {
            return;
        }

        playerHealth.HealthChanged += Refresh;
        Refresh(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged -= Refresh;
        }
    }

    private void Refresh(int currentHealth, int maxHealth)
    {
        int heartLimit = Mathf.Min(maxHealth, hearts.Length);
        int visibleHearts = Mathf.Clamp(currentHealth, 0, heartLimit);

        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] != null)
            {
                hearts[i].enabled = i < visibleHearts;
            }
        }
    }
}
