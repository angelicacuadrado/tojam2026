using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MessageEntry
{
    [TextArea(1, 4)] public string body;
    [Tooltip("Seconds to wait after the previous message (or after chapter start, for the first one) before delivery.")]
    public float delayAfterPrevious = 3f;

    [Header("Optional CTA Button")]
    [Tooltip("Leave blank for a plain message. If set, the bubble shows a button with this label.")]
    public string buttonLabel;
    [Tooltip("Window prefab to open when the bubble's CTA button is clicked.")]
    public GameObject windowPrefabToOpen;
}

