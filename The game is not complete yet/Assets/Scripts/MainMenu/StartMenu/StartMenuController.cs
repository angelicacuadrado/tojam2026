using UnityEngine;
using UnityEngine.UI;

public class StartMenuController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button startButton;
    [SerializeField] private GameObject startMenuPanel;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button shutdownButton;

    [Header("Windows")]
    [SerializeField] private GameObject settingsWindowPrefab;

    [Header("Behavior")]
    [Tooltip("Optional: a fullscreen click-catcher with a StartMenuBackdrop component. Toggled with the menu.")]
    [SerializeField] private GameObject backdrop;

    public bool IsMenuVisible => startMenuPanel != null && startMenuPanel.activeSelf;

    private void Awake()
    {
        if (startMenuPanel != null) startMenuPanel.SetActive(false);
        if (backdrop != null) backdrop.SetActive(false);

        if (startButton != null) startButton.onClick.AddListener(ToggleMenu);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (shutdownButton != null) shutdownButton.onClick.AddListener(Shutdown);
    }

    public void ToggleMenu() => SetMenuVisible(!IsMenuVisible);

    public void SetMenuVisible(bool visible)
    {
        if (startMenuPanel != null) startMenuPanel.SetActive(visible);
        if (backdrop != null) backdrop.SetActive(visible);
    }

    public void OpenSettings()
    {
        SetMenuVisible(false);
        if (settingsWindowPrefab != null && WindowManager.Instance != null)
        {
            WindowManager.Instance.Open(settingsWindowPrefab);
        }
    }

    public void Shutdown()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
