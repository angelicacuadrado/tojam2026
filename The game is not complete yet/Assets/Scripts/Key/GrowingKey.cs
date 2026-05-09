using UnityEngine;

public class GrowingKey : Key, IAttackable
{
    void TakeDamage (float damage)
    {
        // Implement logic to reduce the key's health or trigger a reaction
        Debug.Log($"GrowingKey took {damage} damage!");
    }
}
