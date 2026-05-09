using System;
using UnityEngine;

public class WindowControls : MonoBehaviour
{
    public RectTransform content;
    public RectTransform window;

    [Header("Window Identity")]
    [Tooltip("Unique id for singleton-style windows (e.g. \"settings\"). Leave blank for non-singletons.")]
    public string windowId;
    public string windowTitle = "Window";
    public Sprite windowIcon;
    [Tooltip("If true, opening the same prefab again just focuses the existing window.")]
    public bool isSingleton;
    [Tooltip("If true, Close hides the window instead of destroying it. WindowManager will revive it on next Open.")]
    public bool preserveOnClose;
    [Tooltip("Header height used as collapsed size when minimized.")]
    public float headerHeight = 30f;

    public event Action<WindowControls> Minimized;
    public event Action<WindowControls> RestoredFromMinimize;
    public event Action<WindowControls> Closing;

    public bool IsMinimized => minimized;
    public bool IsMaximized => maximized;

    private struct RectState
    {
        public Vector2 anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta, offsetMin, offsetMax;

        public static RectState Capture(RectTransform rt) => new()
        {
            anchorMin = rt.anchorMin,
            anchorMax = rt.anchorMax,
            pivot = rt.pivot,
            anchoredPosition = rt.anchoredPosition,
            sizeDelta = rt.sizeDelta,
            offsetMin = rt.offsetMin,
            offsetMax = rt.offsetMax,
        };

        public void ApplyTo(RectTransform rt)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }
    }

    private RectState normalState;        // pre-maximize rect; used by RestoreFromMaximize
    private bool normalStateCaptured;
    private RectState minimizeRestoreState; // captured at Minimize; used by RestoreFromMinimize
    private bool minimizeRestoreCaptured;
    private bool minimized;
    private bool maximized;

    private void Awake()
    {
        if (window == null) window = transform as RectTransform;
        if (window != null)
        {
            normalState = RectState.Capture(window);
            normalStateCaptured = true;
        }
    }

    public void Initialize()
    {
        if (content == null) return;
        var rt = content.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public void ToggleMinimize()
    {
        if (minimized) RestoreFromMinimize();
        else Minimize();
    }

    public void Minimize()
    {
        if (window == null || minimized) return;

        // Capture current rect (could be normal OR maximized) so restore returns to the same look.
        // The maximized flag stays as-is — it's preserved across the minimize cycle.
        minimizeRestoreState = RectState.Capture(window);
        minimizeRestoreCaptured = true;

        minimized = true;
        Minimized?.Invoke(this);
        window.gameObject.SetActive(false);
    }

    public void RestoreFromMinimize()
    {
        if (!minimized) return;

        if (window != null)
        {
            window.gameObject.SetActive(true);
            if (minimizeRestoreCaptured) minimizeRestoreState.ApplyTo(window);
        }

        minimized = false;
        RestoredFromMinimize?.Invoke(this);
    }

    public void ToggleMaximize()
    {
        if (minimized) return;
        if (maximized) RestoreFromMaximize();
        else Maximize();
    }

    public void Maximize()
    {
        if (window == null || maximized || minimized) return;

        normalState = RectState.Capture(window);
        normalStateCaptured = true;

        window.anchorMin = Vector2.zero;
        window.anchorMax = Vector2.one;
        window.pivot = new Vector2(0.5f, 0.5f);
        window.offsetMin = Vector2.zero;
        window.offsetMax = Vector2.zero;

        maximized = true;
    }

    public void RestoreFromMaximize()
    {
        if (!maximized || minimized) return;
        if (normalStateCaptured) normalState.ApplyTo(window);
        maximized = false;
    }

    public void Close()
    {
        Closing?.Invoke(this);
        if (!preserveOnClose)
        {
            Destroy(window != null ? window.gameObject : gameObject);
        }
        // If preserveOnClose, WindowManager listens to Closing and stashes the instance.
    }
}
