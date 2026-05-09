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

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
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
        Vector3 move = (player.transform.right * moveInput.x + player.transform.forward * moveInput.y)
            * playerController.MoveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + move);
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
}