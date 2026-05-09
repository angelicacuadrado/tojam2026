using UnityEngine;

public class KeyMovement : MonoBehaviour
{
    [SerializeField, Tooltip("The player transform that the key will follow")]
    private Transform player;
    [SerializeField, Tooltip("Speed at which the key follows the player")]
    private float followSpeed = 5f;
    [SerializeField, Tooltip("Distance the key maintains from the player")]
    private float followDistance = 1.5f;

    private void Update()
    {
        if (player == null)
            return;

        // Desired position: behind the player at a fixed distance
        Vector3 targetPos = player.position - player.forward * followDistance;

        // Smooth follow
        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);
    }
}