using TMPro;
using UnityEngine;

/// <summary>
/// Centered timestamp marker shown between chapters in the message feed.
/// Pair with a Sliced background Image; the rect is auto-sized to fit the text.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class TimeBubble : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    [Header("Auto-size")]
    [SerializeField] private float minWidth = 100f;
    [SerializeField] private float maxWidth = 300f;

    private RectTransform _rect;
    private RectTransform Rect => _rect != null ? _rect : (_rect = (RectTransform)transform);

    public float Height => Rect.sizeDelta.y;

    public void Set(string text)
    {
        if (label != null) label.text = text ?? string.Empty;
        Refit();
    }

    public void Refit()
    {
        if (label == null) return;

        // Padding (left+right inset of label inside the bubble) read from the label's
        // stretch anchors — same trick MessageBubble uses.
        var labelRect = (RectTransform)label.transform;
        float padX = labelRect.anchorMin.x != labelRect.anchorMax.x ? -labelRect.sizeDelta.x : 0f;

        var preferred = label.GetPreferredValues(label.text ?? string.Empty, Mathf.Infinity, Mathf.Infinity);
        float width = Mathf.Clamp(preferred.x + padX, minWidth, maxWidth);
        Rect.sizeDelta = new Vector2(width, Rect.sizeDelta.y);
    }
}
