using UnityEngine;
using UnityEngine.InputSystem;

public class KeyMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject player;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Rigidbody rb;

    [Header("Movement Settings")]
    private Vector2 moveInput;
    private bool jumpQueued;

    [SerializeField] private Transform respawnPos;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    private void Start()
    {
        PlayerHealth.OnPlayerRespawn.AddListener(Respawn);
    }

    private void OnDestroy()
    {
        PlayerHealth.OnPlayerRespawn.RemoveListener(Respawn);
    }

    // Called by PlayerInput
    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            jumpQueued = true;
    }

    private void FixedUpdate()
    {
        HandleMovement();
        HandleJump();
    }

    private void HandleMovement()
    {
        // Get the player's REAL velocity
        Vector3 playerVel = playerController.Rb.linearVelocity;

        // If the player is not moving, the key should not move
        if (playerVel.sqrMagnitude < 0.0001f)
        {
            // Keep vertical velocity (jumping) but stop horizontal drift
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }

        // Copy ONLY the horizontal movement of the player
        Vector3 keyVel = new Vector3(
            playerVel.x,
            rb.linearVelocity.y,   // keep key's own vertical velocity
            playerVel.z
        );

        rb.linearVelocity = keyVel;
    }

    private void HandleJump()
    {
        // Check if player is grounded by accessing PlayerController's IsGrounded property
        if (jumpQueued && playerController.IsGrounded)
        {
            rb.AddForce(Vector3.up * playerController.JumpForce, ForceMode.Impulse);
        }

        jumpQueued = false; // consume jump
    }

    private void Respawn()
    {
        // Reset position to player's position
        transform.position = respawnPos.position;
        rb.linearVelocity = Vector3.zero; // reset velocity
    }
}