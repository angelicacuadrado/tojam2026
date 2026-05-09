using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MessageWindow : MonoBehaviour
{
    [SerializeField] private MessageBubble bubblePrefab;

    [Header("Contacts — left-side buttons")]
    [SerializeField] private Button devMessage;
    [SerializeField] private Button person1Message;
    [SerializeField] private Button person2Message;

    [Header("Contacts — right-side panels")]
    [SerializeField] private GameObject devMessageContant;
    [SerializeField] private GameObject person1MessageContant;
    [SerializeField] private GameObject person2MessageContant;

    [Header("Dev (live) — scroll refs")]
    [FormerlySerializedAs("messageContainer")]
    [SerializeField] private RectTransform devMessageContainer;
    [FormerlySerializedAs("scrollRect")]
    [SerializeField] private ScrollRect devScrollRect;

    [Header("Person 1 (preset)")]
    [SerializeField] private RectTransform person1MessageContainer;
    [SerializeField] private ScrollRect person1ScrollRect;
    [SerializeField] private MessageEntry[] person1Messages;

    [Header("Person 2 (preset)")]
    [SerializeField] private RectTransform person2MessageContainer;
    [SerializeField] private ScrollRect person2ScrollRect;
    [SerializeField] private MessageEntry[] person2Messages;

    [Header("Indicators")]
    [Tooltip("Red dot on Dev's contact button. Shown only while window is open and Dev isn't selected.")]
    [SerializeField] private GameObject devUnreadDot;

    [Header("Layout")]
    [SerializeField] private float topPadding = 20f;
    [SerializeField] private float bottomPadding = 20f;
    [SerializeField] private float leftPadding = 20f;
    [Tooltip("Vertical gap between consecutive bubbles.")]
    [SerializeField] private float bubbleSpacing = 20f;

    private bool _devSelected;
    private float _devNextY;

    private void OnEnable()
    {
        _devSelected = false;
        SetActiveSafe(devMessageContant, false);
        SetActiveSafe(person1MessageContant, false);
        SetActiveSafe(person2MessageContant, false);
        SetActiveSafe(devUnreadDot, false);

        WireButton(devMessage, SelectDev);
        WireButton(person1Message, SelectPerson1);
        WireButton(person2Message, SelectPerson2);

        BuildStaticPanel(person1MessageContainer, person1Messages);
        BuildStaticPanel(person2MessageContainer, person2Messages);
        BuildDevPanel();

        var scheduler = MessageScheduler.Instance;
        if (scheduler != null)
        {
            scheduler.MessageDelivered += OnDevMessageDelivered;
            scheduler.SuppressNotificationSound = false;
        }
    }

    private void OnDisable()
    {
        var scheduler = MessageScheduler.Instance;
        if (scheduler != null)
        {
            scheduler.MessageDelivered -= OnDevMessageDelivered;
            scheduler.SuppressNotificationSound = false;
        }
    }

    private void BuildDevPanel()
    {
        ClearContainer(devMessageContainer);
        _devNextY = -topPadding;
        UpdateContentHeight(devMessageContainer, _devNextY);

        var scheduler = MessageScheduler.Instance;
        if (scheduler == null) return;
        foreach (var entry in scheduler.Delivered)
        {
            SpawnDevBubble(entry);
        }
    }

    private void OnDevMessageDelivered(MessageEntry entry)
    {
        SpawnDevBubble(entry);
        if (_devSelected)
        {
            ScrollToBottom(devScrollRect);
        }
        else
        {
            SetActiveSafe(devUnreadDot, true);
        }
    }

    private void SelectDev()
    {
        _devSelected = true;
        SetActiveSafe(devMessageContant, true);
        SetActiveSafe(person1MessageContant, false);
        SetActiveSafe(person2MessageContant, false);
        SetActiveSafe(devUnreadDot, false);
        SetSuppressNotification(true);
        ScrollToBottom(devScrollRect);
    }

    private void SelectPerson1() => ShowOther(person1MessageContant, person1ScrollRect);
    private void SelectPerson2() => ShowOther(person2MessageContant, person2ScrollRect);

    private void ShowOther(GameObject panel, ScrollRect scroll)
    {
        _devSelected = false;
        SetActiveSafe(devMessageContant, false);
        SetActiveSafe(person1MessageContant, false);
        SetActiveSafe(person2MessageContant, false);
        SetActiveSafe(panel, true);
        SetSuppressNotification(false);
        ScrollToBottom(scroll);
    }

    private void SpawnDevBubble(MessageEntry entry)
    {
        if (bubblePrefab == null || devMessageContainer == null) return;
        var bubble = Instantiate(bubblePrefab, devMessageContainer);
        bubble.Set(entry);
        var rect = (RectTransform)bubble.transform;
        rect.anchoredPosition = new Vector2(leftPadding, _devNextY);
        _devNextY -= bubble.TotalHeight + bubbleSpacing;
        UpdateContentHeight(devMessageContainer, _devNextY);
    }

    private void BuildStaticPanel(RectTransform container, IReadOnlyList<MessageEntry> entries)
    {
        if (container == null) return;
        ClearContainer(container);
        float nextY = -topPadding;
        if (entries != null && bubblePrefab != null)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null) continue;
                var bubble = Instantiate(bubblePrefab, container);
                bubble.Set(entry);
                var rect = (RectTransform)bubble.transform;
                rect.anchoredPosition = new Vector2(leftPadding, nextY);
                nextY -= bubble.TotalHeight + bubbleSpacing;
            }
        }
        UpdateContentHeight(container, nextY);
    }

    private void ClearContainer(RectTransform container)
    {
        if (container == null) return;
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Destroy(container.GetChild(i).gameObject);
        }
    }

    private void UpdateContentHeight(RectTransform container, float nextY)
    {
        if (container == null) return;
        float used = -nextY - bubbleSpacing + bottomPadding;
        if (used < topPadding + bottomPadding) used = topPadding + bottomPadding;
        var size = container.sizeDelta;
        container.sizeDelta = new Vector2(size.x, used);
    }

    private void ScrollToBottom(ScrollRect scroll)
    {
        if (scroll == null) return;
        Canvas.ForceUpdateCanvases();
        scroll.verticalNormalizedPosition = 0f;
    }

    private static void SetSuppressNotification(bool suppress)
    {
        if (MessageScheduler.Instance != null)
            MessageScheduler.Instance.SuppressNotificationSound = suppress;
    }

    private static void WireButton(Button button, UnityEngine.Events.UnityAction handler)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(handler);
    }

    private static void SetActiveSafe(GameObject obj, bool active)
    {
        if (obj != null) obj.SetActive(active);
    }
}
