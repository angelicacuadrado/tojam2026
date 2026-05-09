using UnityEngine;
using UnityEngine.UI;

public class MessageWindow : MonoBehaviour
{
    [SerializeField] private MessageBubble bubblePrefab;
    [SerializeField] private RectTransform messageContainer;
    [SerializeField] private ScrollRect scrollRect;

    private void OnEnable()
    {
        ClearContainer();

        var scheduler = MessageScheduler.Instance;
        if (scheduler == null) return;

        foreach (var entry in scheduler.Delivered)
        {
            SpawnBubble(entry, scrollImmediate: true);
        }

        scheduler.MessageDelivered += OnMessageDelivered;
        ScrollToBottom();
    }

    private void OnDisable()
    {
        if (MessageScheduler.Instance != null)
        {
            MessageScheduler.Instance.MessageDelivered -= OnMessageDelivered;
        }
    }

    private void OnMessageDelivered(MessageEntry entry)
    {
        SpawnBubble(entry, scrollImmediate: false);
        ScrollToBottom();
    }

    private void SpawnBubble(MessageEntry entry, bool scrollImmediate)
    {
        if (bubblePrefab == null || messageContainer == null) return;
        var bubble = Instantiate(bubblePrefab, messageContainer);
        bubble.Set(entry);
    }

    private void ClearContainer()
    {
        if (messageContainer == null) return;
        for (int i = messageContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(messageContainer.GetChild(i).gameObject);
        }
    }

    private void ScrollToBottom()
    {
        if (scrollRect == null) return;
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}
