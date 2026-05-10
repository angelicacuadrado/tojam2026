using UnityEngine;
using UnityEngine.Serialization;

public class DoorAnimationController : MonoBehaviour
{
    [SerializeField] private bool isOpen = false;

    [Header("Visual State")]
    [FormerlySerializedAs("exitRenderer")]
    [SerializeField] private Renderer indicatorRenderer;
    [SerializeField] private Collider doorCollider;
    [SerializeField] private Material closedMat;
    [SerializeField] private Material openMat;

    [Header("Door Animation")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string openStateName = "OpenDoor";
    [SerializeField] private int animationLayer = 0;
    [SerializeField] private float animationSpeed = 1f;
    [SerializeField, Range(0f, 1f)] private float closedNormalizedTime = 0f;
    [SerializeField, Range(0f, 1f)] private float openNormalizedTime = 1f;

    private float currentNormalizedTime;
    private float targetNormalizedTime;
    private bool isAnimatingDoor;

    private void Awake()
    {
        if (doorCollider == null)
            doorCollider = GetComponent<Collider>();

        if (doorAnimator == null)
            doorAnimator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        currentNormalizedTime = isOpen ? openNormalizedTime : closedNormalizedTime;
        targetNormalizedTime = currentNormalizedTime;
        ApplyDoorState();
        SampleDoorAnimation();
    }

    private void Update()
    {
        if (!isAnimatingDoor || doorAnimator == null)
            return;

        float step = Mathf.Abs(animationSpeed) * Time.deltaTime;
        currentNormalizedTime = Mathf.MoveTowards(currentNormalizedTime, targetNormalizedTime, step);
        SampleDoorAnimation();

        if (Mathf.Approximately(currentNormalizedTime, targetNormalizedTime))
            isAnimatingDoor = false;
    }

    public void OpenDoor()
    {
        isOpen = true;
        ApplyDoorState();
        MoveDoorAnimationTo(openNormalizedTime);
    }

    public void CloseDoor()
    {
        isOpen = false;
        ApplyDoorState();
        MoveDoorAnimationTo(closedNormalizedTime);
    }

    private void ApplyDoorState()
    {
        if (indicatorRenderer != null)
            indicatorRenderer.material = isOpen ? openMat : closedMat;

        if (doorCollider != null)
            doorCollider.isTrigger = isOpen;
    }

    private void MoveDoorAnimationTo(float normalizedTime)
    {
        if (doorAnimator == null)
            return;

        targetNormalizedTime = Mathf.Clamp01(normalizedTime);

        if (Mathf.Approximately(currentNormalizedTime, targetNormalizedTime))
        {
            isAnimatingDoor = false;
            SampleDoorAnimation();
            return;
        }

        isAnimatingDoor = true;
    }

    private void SampleDoorAnimation()
    {
        if (doorAnimator == null || string.IsNullOrWhiteSpace(openStateName))
            return;

        doorAnimator.speed = 0f;
        doorAnimator.Play(openStateName, animationLayer, Mathf.Clamp01(currentNormalizedTime));
        doorAnimator.Update(0f);
    }
}
