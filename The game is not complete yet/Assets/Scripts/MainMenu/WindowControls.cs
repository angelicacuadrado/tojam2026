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

    private RectState normalState;
    private bool normalStateCaptured;
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
        if (maximized) RestoreFromMaximize();

        if (!normalStateCaptured)
        {
            normalState = RectState.Capture(window);
            normalStateCaptured = true;
        }
        else
        {
            normalState = RectState.Capture(window);
        }

        if (content != null) content.gameObject.SetActive(false);

        // Force pivot-friendly anchors so the visible height collapses to header.
        Vector2 currentSize = window.rect.size;
        Vector2 anchored = window.anchoredPosition;
        window.anchorMin = window.anchorMax = new Vector2(0.5f, 0.5f);
        window.pivot = new Vector2(0.5f, 0.5f);
        window.anchoredPosition = anchored;
        window.sizeDelta = new Vector2(currentSize.x, headerHeight);

        minimized = true;
        Minimized?.Invoke(this);
    }

    public void RestoreFromMinimize()
    {
        if (!minimized) return;
        if (content != null) content.gameObject.SetActive(true);
        if (normalStateCaptured) normalState.ApplyTo(window);

        minimized = false;
        RestoredFromMinimize?.Invoke(this);
    }

    public void ToggleMaximize()
    {
        if (maximized) RestoreFromMaximize();
        else Maximize();
    }

    public void Maximize()
    {
        if (window == null || maximized) return;
        if (minimized) RestoreFromMinimize();

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
        if (!maximized) return;
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
