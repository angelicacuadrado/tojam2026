using System.Collections.Generic;
using UnityEngine;

public class TaskbarController : MonoBehaviour
{
    [SerializeField] private TaskbarButton buttonPrefab;
    [SerializeField] private RectTransform buttonContainer;

    private readonly Dictionary<WindowControls, TaskbarButton> buttons = new();

    private void OnEnable()
    {
        if (WindowManager.Instance != null) Subscribe(WindowManager.Instance);
        else StartCoroutine(WaitForManager());
    }

    private System.Collections.IEnumerator WaitForManager()
    {
        while (WindowManager.Instance == null) yield return null;
        Subscribe(WindowManager.Instance);
    }

    private void OnDisable()
    {
        if (WindowManager.Instance != null) Unsubscribe(WindowManager.Instance);
    }

    private void Subscribe(WindowManager m)
    {
        m.WindowOpened += OnOpened;
        m.WindowClosed += OnClosed;
        m.WindowMinimized += OnStateChanged;
        m.WindowRestored += OnStateChanged;
    }

    private void Unsubscribe(WindowManager m)
    {
        m.WindowOpened -= OnOpened;
        m.WindowClosed -= OnClosed;
        m.WindowMinimized -= OnStateChanged;
        m.WindowRestored -= OnStateChanged;
    }

    private void OnOpened(WindowControls window)
    {
        if (buttons.ContainsKey(window) || buttonPrefab == null || buttonContainer == null) return;

        var btn = Instantiate(buttonPrefab, buttonContainer);
        btn.Bind(window);
        buttons[window] = btn;
    }

    private void OnClosed(WindowControls window)
    {
        if (!buttons.TryGetValue(window, out var btn)) return;
        if (btn != null) Destroy(btn.gameObject);
        buttons.Remove(window);
    }

    private void OnStateChanged(WindowControls window)
    {
        if (buttons.TryGetValue(window, out var btn) && btn != null)
        {
            btn.Refresh();
        }
    }
}
