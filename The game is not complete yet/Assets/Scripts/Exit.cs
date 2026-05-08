using UnityEngine;
using UnityEngine.Events;

public class Exit : MonoBehaviour
{
    public static Exit Instance { get; private set; }

    [SerializeField] private bool isOpen = false;

    [SerializeField] private Renderer exitRenderer;
    [SerializeField] private Collider exitCollider;
    [SerializeField] private Material closedMat;
    [SerializeField] private Material openMat;

    [HideInInspector] public UnityEvent ExitLevel;

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

        if (exitRenderer == null) { exitRenderer = GetComponent<Renderer>(); }
        if (exitRenderer == null) {
            Debug.LogWarning("Exit: Renderer component not found on the GameObject. Please assign it in the inspector.");
        }
        exitCollider = GetComponent<Collider>();
        if (exitCollider == null) {
            Debug.LogWarning("Exit: Collider component not found on the GameObject. Please assign it in the inspector.");
        }
    }

    private void Start()
    {
        GameManager.Instance.OpenExit.AddListener(Open);

        // Redundancy to ensure the exit starts in the correct state
        isOpen = false;
        exitRenderer.material = closedMat;
        exitCollider.isTrigger = false;
    }

    private void Open()
    {
        isOpen = true;
        exitRenderer.material = openMat;
        exitCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isOpen && other.CompareTag("Player"))
        {
            Debug.Log("Level Completed!");

            ExitLevel.Invoke();
        }
    }
}