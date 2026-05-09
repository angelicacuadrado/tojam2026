using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public abstract class PressureButtonBase : MonoBehaviour
{
    [Header("Pressure")]
    [SerializeField] private LayerMask pressableLayers = ~0;

    [Header("Events")]
    [SerializeField] private UnityEvent onPressed;

    [Header("Optional Visual")]
    [SerializeField] private Transform buttonVisual;
    [SerializeField] private Vector3 pressedLocalOffset = new Vector3(0f, -0.1f, 0f);
    [SerializeField] private float visualMoveSpeed = 8f;

    private Vector3 releasedLocalPosition;
    private Vector3 pressedLocalPosition;
    private bool hasVisual;

    public bool IsPressed { get; private set; }

    private void Reset()
    {
        if (TryGetComponent(out Collider triggerCollider))
            triggerCollider.isTrigger = true;
    }

    protected virtual void Awake()
    {
        hasVisual = buttonVisual != null;

        if (!hasVisual)
            return;

        releasedLocalPosition = buttonVisual.localPosition;
        pressedLocalPosition = releasedLocalPosition + pressedLocalOffset;
    }

    private void Update()
    {
        if (!hasVisual)
            return;

        Vector3 targetPosition = IsPressed ? pressedLocalPosition : releasedLocalPosition;
        buttonVisual.localPosition = Vector3.MoveTowards(
            buttonVisual.localPosition,
            targetPosition,
            visualMoveSpeed * Time.deltaTime
        );
    }

    protected bool IsValidPressureSource(Collider other)
    {
        int otherLayerMask = 1 << other.gameObject.layer;
        return (pressableLayers.value & otherLayerMask) != 0;
    }

    protected void SetPressed(bool pressed)
    {
        if (IsPressed == pressed)
            return;

        IsPressed = pressed;

        if (IsPressed)
            OnPressed();
        else
            OnReleased();
    }

    protected virtual void OnPressed()
    {
        Debug.Log($"{name} pressed.", this);
        onPressed?.Invoke();
    }

    protected virtual void OnReleased()
    {
        Debug.Log($"{name} released.", this);
    }
}
