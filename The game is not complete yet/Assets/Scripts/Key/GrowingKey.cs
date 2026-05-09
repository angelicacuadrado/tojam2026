using UnityEngine;

public class GrowingKey : Key, IAttackable
{
    [SerializeField] private int maxScaleTimes = 10;
    [SerializeField] private int currentScaleTimes = 0;
    [SerializeField] private float growthMultiplier = 1.2f;

    public void TakeDamage(int damage)
    {
        if (damage > 0)
        {
            Grow();
        }
    }

    public void Grow()
    {
        if (currentScaleTimes >= maxScaleTimes) return;
        currentScaleTimes++;

        if (growthMultiplier <= 0f) return;

        transform.localScale = transform.localScale * growthMultiplier;
    }
}