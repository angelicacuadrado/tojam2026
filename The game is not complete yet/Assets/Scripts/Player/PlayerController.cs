using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    private float footstepInterval = 0.5f;
    private Vector3 platformVelocity;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask groundLayer = ~0;
    private bool isGrounded;
    private float footstepTimer;

    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private Transform cameraTransform;

    private Rigidbody rb;

    // Input values
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpPressed;
    private bool isWalking = false;

    private float xRotation = 0f;

    // Properties
    public float MoveSpeed => moveSpeed;
    public float JumpForce => jumpForce;
    public bool IsGrounded => isGrounded;
    public Rigidbody Rb => rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // Prevent physics rotation
    }

    private void Update()
    {
        HandleLook();
        HandleWalkAudio();
    }

    private void FixedUpdate()
    {
        HandleGroundAndPlatformCheck();
        HandleMovement();
        HandleJump();
    }

    // -----------------------------
    // INPUT SYSTEM CALLBACKS
    // -----------------------------

    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext ctx)
    {
        lookInput = ctx.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            jumpPressed = true;
    }

    // -----------------------------
    // MOVEMENT
    // -----------------------------

    private void HandleMovement()
    {
        // Convert input to world-space movement direction
        Vector3 moveDir = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;

        // Keep existing vertical velocity (gravity, jumping)
        Vector3 currentVel = rb.linearVelocity;

        // Replace horizontal velocity with our movement
        Vector3 targetVel = new Vector3(
            moveDir.x * moveSpeed + platformVelocity.x,
            currentVel.y,
            moveDir.z * moveSpeed + platformVelocity.z
        );

        rb.linearVelocity = targetVel;
    }

    private void HandleWalkAudio()
    {
        if (isGrounded && moveInput.magnitude > 0.1f)
        {
            if (!isWalking)
            {
                if (NarratorController.Instance != null)
                {
                    NarratorController.Instance.PlayLine("Level1_2");
                }
                isWalking = true;
            }

            footstepTimer += Time.deltaTime;
            if (footstepTimer >= footstepInterval)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX("PlayerWalk");
                }
                footstepTimer = 0f;
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    // -----------------------------
    // JUMP
    // -----------------------------

    private void HandleGroundAndPlatformCheck()
    {
        platformVelocity = Vector3.zero;

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer))
        {
            isGrounded = true;

            var platform = hit.collider.GetComponent<WalkablePlatform>();
            if (platform != null)
                platformVelocity = platform.Veclocity;
        }
        else
        {
            isGrounded = false;
        }
    }

    private void HandleJump()
    {
        if (jumpPressed && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            AudioManager.Instance.PlaySFX("PlayerJump");
        }

        jumpPressed = false; // consume jump input
    }

    // -----------------------------
    // LOOK
    // -----------------------------

    private void HandleLook()
    {
        float sensitivity = mouseSensitivity * (SettingsManager.MouseSensitivity / SettingsManager.DefaultMouseSensitivity);
        float mouseX = lookInput.x * sensitivity * Time.deltaTime;
        float mouseY = lookInput.y * sensitivity * Time.deltaTime;

        // Rotate player horizontally
        transform.Rotate(Vector3.up * mouseX);

        // Rotate camera vertically
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
    }
}
