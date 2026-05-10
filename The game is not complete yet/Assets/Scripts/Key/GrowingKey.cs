using UnityEngine;

public class GrowingKey : Key, IAttackable
{
    [SerializeField] private int maxScaleTimes = 10;
    [SerializeField] private int currentScaleTimes = 0;
    [SerializeField] private float growthMultiplier = 1.2f;
    private bool firstKey = false;

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

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.Play3DSFX("KeyGrow", transform.position);
        }
    }

    public override void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        foreach (string tagToCheck in tagsToCheck)
        {
            if (other.CompareTag(tagToCheck))
            {
                isCollected = true;
                GameManager.Instance.AddKey();

                if (!firstKey)
                {
                    firstKey = true;
                    NarratorController.Instance?.PlayLine("Level2_3");
                }
                AudioManager.Instance?.Play3DSFX("KeyPickup", transform.position);
                poolOwner.ReturnToPool(gameObject, poolKey);

                break;
            }
        }
    }
}