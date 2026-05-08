using UnityEngine;

public class EnemyGrower : MonoBehaviour
{
    [SerializeField] private float maxScaleMultiplier = 3f;

    private Vector3 initialScale;

    private void Awake()
    {
        initialScale = transform.localScale;
    }

    public void Grow(float multiplier)
    {
        if (multiplier <= 0f)
            return;

        Vector3 targetScale = transform.localScale * multiplier;

        if (maxScaleMultiplier > 0f)
        {
            Vector3 maxScale = initialScale * maxScaleMultiplier;
            targetScale = new Vector3(
                Mathf.Min(targetScale.x, maxScale.x),
                Mathf.Min(targetScale.y, maxScale.y),
                Mathf.Min(targetScale.z, maxScale.z)
            );
        }

        transform.localScale = targetScale;
    }

    public void ResetScale()
    {
        transform.localScale = initialScale;
    }
}
