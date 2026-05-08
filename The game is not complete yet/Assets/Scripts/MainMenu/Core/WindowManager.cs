using System;
using System.Collections.Generic;
using UnityEngine;

public class WindowManager : MonoBehaviour
{
    public static WindowManager Instance { get; private set; }

    [Header("Where opened windows are parented (e.g. Canvas/Windows)")]
    [SerializeField] private RectTransform windowRoot;

    public event Action<WindowControls> WindowOpened;
    public event Action<WindowControls> WindowClosed;
    public event Action<WindowControls> WindowMinimized;
    public event Action<WindowControls> WindowRestored;

    private readonly List<WindowControls> openWindows = new();
    private readonly Dictionary<string, WindowControls> singletonWindows = new();
    private readonly Dictionary<string, WindowControls> preservedWindows = new();

    public IReadOnlyList<WindowControls> OpenWindows => openWindows;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        if (windowRoot == null)
        {
            windowRoot = transform as RectTransform;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public WindowControls Open(GameObject windowPrefab)
    {
        if (windowPrefab == null) return null;

        var prefabControls = windowPrefab.GetComponent<WindowControls>();
        string id = prefabControls != null ? prefabControls.windowId : null;

        if (prefabControls != null && prefabControls.isSingleton
            && !string.IsNullOrEmpty(id)
            && singletonWindows.TryGetValue(id, out var existing)
            && existing != null)
        {
            if (existing.IsMinimized) existing.RestoreFromMinimize();
            BringToFront(existing);
            return existing;
        }

        if (!string.IsNullOrEmpty(id)
            && preservedWindows.TryGetValue(id, out var preserved)
            && preserved != null && preserved.window != null)
        {
            preservedWindows.Remove(id);
            preserved.window.gameObject.SetActive(true);
            if (preserved.IsMinimized) preserved.RestoreFromMinimize();
            Register(preserved);
            return preserved;
        }

        var instance = Instantiate(windowPrefab, windowRoot);
        var controls = instance.GetComponent<WindowControls>();
        if (controls == null)
        {
            Debug.LogError($"[WindowManager] Prefab '{windowPrefab.name}' has no WindowControls component.");
            Destroy(instance);
            return null;
        }

        Register(controls);
        return controls;
    }

    public void Register(WindowControls window)
    {
        if (window == null || openWindows.Contains(window)) return;

        openWindows.Add(window);
        if (window.isSingleton && !string.IsNullOrEmpty(window.windowId))
        {
            singletonWindows[window.windowId] = window;
        }

        window.Minimized += HandleMinimized;
        window.RestoredFromMinimize += HandleRestored;
        window.Closing += HandleClosing;

        BringToFront(window);
        WindowOpened?.Invoke(window);
    }

    public void BringToFront(WindowControls window)
    {
        if (window == null || window.window == null) return;
        window.window.SetAsLastSibling();
    }

    public void RestoreOrFocus(WindowControls window)
    {
        if (window == null) return;
        if (window.IsMinimized) window.RestoreFromMinimize();
        else BringToFront(window);
    }

    public void ToggleMinimize(WindowControls window)
    {
        if (window == null) return;
        if (window.IsMinimized) window.RestoreFromMinimize();
        else window.Minimize();
    }

    private void HandleMinimized(WindowControls window) => WindowMinimized?.Invoke(window);
    private void HandleRestored(WindowControls window)
    {
        BringToFront(window);
        WindowRestored?.Invoke(window);
    }

    private void HandleClosing(WindowControls window)
    {
        window.Minimized -= HandleMinimized;
        window.RestoredFromMinimize -= HandleRestored;
        window.Closing -= HandleClosing;

        openWindows.Remove(window);
        if (window.isSingleton && !string.IsNullOrEmpty(window.windowId))
        {
            singletonWindows.Remove(window.windowId);
        }

        if (window.preserveOnClose && !string.IsNullOrEmpty(window.windowId) && window.window != null)
        {
            window.window.gameObject.SetActive(false);
            preservedWindows[window.windowId] = window;
        }

        WindowClosed?.Invoke(window);
    }
}
