using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class Exit : MonoBehaviour
{
    public static Exit Instance { get; private set; }

    [SerializeField] private bool isOpen = false;

    [Header("Visual State")]
    [FormerlySerializedAs("exitRenderer")]
    [SerializeField] private Renderer indicatorRenderer;
    [SerializeField] private Collider exitCollider;
    [SerializeField] private Material closedMat;
    [SerializeField] private Material openMat;

    [Header("Door Animation")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string openStateName = "OpenDoor";
    [SerializeField] private int animationLayer = 0;
    [SerializeField] private float animationSpeed = 1f;
    [SerializeField, Range(0f, 1f)] private float closedNormalizedTime = 0f;
    [SerializeField, Range(0f, 1f)] private float openNormalizedTime = 1f;

    [HideInInspector] public UnityEvent ExitLevel;

    /// <summary>Fires whenever any Exit triggers ExitLevel. Cross-scene listeners (e.g. ChapterProgressManager) subscribe here.</summary>
    public static event System.Action<Exit> AnyLevelCompleted;

    private float currentNormalizedTime;
    private float targetNormalizedTime;
    private bool isAnimatingDoor;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        if (indicatorRenderer == null) {
            Debug.LogWarning("Exit: Indicator Renderer is not assigned. Assign the indicator light renderer in the inspector.");
        }
        exitCollider = GetComponent<Collider>();
        if (exitCollider == null) {
            Debug.LogWarning("Exit: Collider component not found on the GameObject. Please assign it in the inspector.");
        }
        if (doorAnimator == null) { doorAnimator = GetComponentInChildren<Animator>(); }
    }

    private void Start()
    {
        GameManager.Instance.OpenExit.AddListener(UnlockExit);

        // Redundancy to ensure the exit starts in the correct state
        isOpen = false;
        ApplyVisualState();
        currentNormalizedTime = closedNormalizedTime;
        targetNormalizedTime = closedNormalizedTime;
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

    public void UnlockExit()
    {
        isOpen = true;
        ApplyVisualState();
        MoveDoorAnimationTo(openNormalizedTime);
    }

    private void ApplyVisualState()
    {
        if (indicatorRenderer != null)
            indicatorRenderer.material = isOpen ? openMat : closedMat;

        if (exitCollider != null)
            exitCollider.isTrigger = isOpen;
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

    private void OnTriggerEnter(Collider other)
    {
        if (isOpen && other.CompareTag("Player"))
        {
            AudioManager.Instance?.PlaySFX("LevelWin");
            LevelHost host = FindFirstObjectByType<LevelHost>();
            if (host != null)
            {
                host._levelCompleted = true;
                host.GetComponent<WindowControls>().Close();
            }

            AnyLevelCompleted?.Invoke(this);
        }
    }
}
