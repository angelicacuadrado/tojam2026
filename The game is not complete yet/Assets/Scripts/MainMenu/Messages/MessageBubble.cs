using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class MessageBubble : MonoBehaviour
{
    [SerializeField] private TMP_Text bodyLabel;

    [Header("Optional CTA")]
    [Tooltip("Activated only when the entry has a buttonLabel + windowPrefabToOpen.")]
    [SerializeField] private GameObject ctaContainer;
    [SerializeField] private Button ctaButton;
    [SerializeField] private TMP_Text ctaLabel;

    [Header("Bubble auto-size")]
    [SerializeField] private float bubbleMinWidth = 160f;
    [SerializeField] private float bubbleMaxWidth = 600f;
    [SerializeField] private float bubbleMinHeight = 80f;

    [Header("CTA auto-size")]
    [Tooltip("Horizontal padding around the CTA label (covers the leading icon + side margins).")]
    [SerializeField] private float ctaPaddingX = 110f;
    [SerializeField] private float ctaMinWidth = 160f;
    [SerializeField] private float ctaMaxWidth = 600f;
    [SerializeField] private float ctaHeight = 50f;
    [Tooltip("Vertical gap between the bubble's bottom edge and the CTA's top edge.")]
    [SerializeField] private float ctaSpacing = 10f;

    private RectTransform _rect;

    private RectTransform Rect => _rect != null ? _rect : (_rect = (RectTransform)transform);

    /// <summary>
    /// Vertical footprint occupied by this bubble in a message list — bubble height
    /// plus the CTA strip if it's visible. Use this when laying out a list of bubbles.
    /// </summary>
    public float TotalHeight
    {
        get
        {
            float h = Rect.sizeDelta.y;
            if (ctaContainer != null && ctaContainer.activeSelf)
            {
                h += ctaSpacing + ctaHeight;
            }
            return h;
        }
    }

    public void Set(MessageEntry entry)
    {
        if (entry == null) return;
        if (bodyLabel != null) bodyLabel.text = entry.body;

        bool hasCta = !string.IsNullOrEmpty(entry.buttonLabel) && entry.windowPrefabToOpen != null;
        if (ctaContainer != null) ctaContainer.SetActive(hasCta);

        if (hasCta && ctaButton != null)
        {
            if (ctaLabel != null) ctaLabel.text = entry.buttonLabel;
            var prefab = entry.windowPrefabToOpen;
            ctaButton.onClick.RemoveAllListeners();
            ctaButton.onClick.AddListener(() =>
            {
                if (WindowManager.Instance != null) WindowManager.Instance.Open(prefab);
            });
        }

        Refit();
    }

    public void Refit()
    {
        float bubbleHeight = ResizeBubble();
        ResizeCta(bubbleHeight);
    }

    private float ResizeBubble()
    {
        var rect = Rect;
        if (bodyLabel == null)
        {
            return rect.sizeDelta.y;
        }

        // Padding is the body's inset relative to the bubble — read it straight off
        // the body's stretch anchors so prefab tweaks stay in sync with the math.
        Vector2 padding = GetBodyPadding();

        string text = bodyLabel.text ?? string.Empty;
        float maxBodyWidth = Mathf.Max(1f, bubbleMaxWidth - padding.x);

        // Step 1: measure unconstrained — gives the natural single-line width.
        Vector2 preferred = bodyLabel.GetPreferredValues(text, Mathf.Infinity, Mathf.Infinity);

        float bubbleWidth;
        if (preferred.x <= maxBodyWidth)
        {
            bubbleWidth = Mathf.Max(preferred.x + padding.x, bubbleMinWidth);
        }
        else
        {
            // Step 2: text would overflow — pin width to max and re-measure with wrapping.
            bubbleWidth = bubbleMaxWidth;
            preferred = bodyLabel.GetPreferredValues(text, maxBodyWidth, Mathf.Infinity);
        }

        float bubbleHeight = Mathf.Max(preferred.y + padding.y, bubbleMinHeight);
        rect.sizeDelta = new Vector2(bubbleWidth, bubbleHeight);
        return bubbleHeight;
    }

    private Vector2 GetBodyPadding()
    {
        var bodyRect = (RectTransform)bodyLabel.transform;
        // For each axis with a stretch anchor (anchorMin != anchorMax), the rect's inset
        // around its parent equals -sizeDelta on that axis. For non-stretch anchors we
        // fall back to 0 — the body's size doesn't track the bubble in that case anyway.
        float padX = bodyRect.anchorMin.x != bodyRect.anchorMax.x ? -bodyRect.sizeDelta.x : 0f;
        float padY = bodyRect.anchorMin.y != bodyRect.anchorMax.y ? -bodyRect.sizeDelta.y : 0f;
        return new Vector2(padX, padY);
    }

    private void ResizeCta(float bubbleHeight)
    {
        if (ctaContainer == null || !ctaContainer.activeSelf) return;
        var ctaRect = ctaContainer.transform as RectTransform;
        if (ctaRect == null) return;

        Vector2 pivot = ctaRect.pivot;
        Vector2 oldSize = ctaRect.sizeDelta;
        Vector2 oldAnchored = ctaRect.anchoredPosition;

        // Preserve the CTA's current left edge relative to the bubble, then grow rightward.
        float leftEdge = oldAnchored.x - pivot.x * oldSize.x;

        float labelWidth = ctaLabel != null
            ? ctaLabel.GetPreferredValues(ctaLabel.text ?? string.Empty, Mathf.Infinity, Mathf.Infinity).x
            : 0f;
        float ctaWidth = Mathf.Clamp(labelWidth + ctaPaddingX, ctaMinWidth, ctaMaxWidth);

        // CTAContainer is anchored to the bubble's top-left, so y is negative going down.
        // Place its top edge `ctaSpacing` below the bubble's bottom (= -bubbleHeight).
        float ctaTopY = -bubbleHeight - ctaSpacing;
        float anchoredX = leftEdge + pivot.x * ctaWidth;
        float anchoredY = ctaTopY - (1f - pivot.y) * ctaHeight;

        ctaRect.sizeDelta = new Vector2(ctaWidth, ctaHeight);
        ctaRect.anchoredPosition = new Vector2(anchoredX, anchoredY);
    }
}
