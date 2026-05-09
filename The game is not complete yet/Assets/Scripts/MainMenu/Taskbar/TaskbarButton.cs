using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TaskbarButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image iconImage;
    [Tooltip("Optional: object enabled when the bound window is minimized.")]
    [SerializeField] private GameObject minimizedIndicator;

    private WindowControls boundWindow;

    public void Bind(WindowControls window)
    {
        boundWindow = window;

        if (label != null) label.text = string.IsNullOrEmpty(window.windowTitle) ? window.name : window.windowTitle;
        if (iconImage != null)
        {
            iconImage.sprite = window.windowIcon;
            iconImage.enabled = window.windowIcon != null;
        }
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClicked);
        }
        Refresh();
    }

    public void Refresh()
    {
        if (boundWindow == null) return;
        if (minimizedIndicator != null) minimizedIndicator.SetActive(boundWindow.IsMinimized);
    }

    private void OnClicked()
    {
        if (boundWindow == null || WindowManager.Instance == null) return;

        if (boundWindow.IsMinimized)
        {
            boundWindow.RestoreFromMinimize();
        }
        else if (boundWindow.window.GetSiblingIndex() == boundWindow.window.parent.childCount - 1)
        {
            boundWindow.Minimize();
        }
        else
        {
            WindowManager.Instance.BringToFront(boundWindow);
        }
    }
}
