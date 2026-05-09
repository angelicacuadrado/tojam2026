using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessageBubble : MonoBehaviour
{
    [SerializeField] private TMP_Text senderLabel;
    [SerializeField] private TMP_Text bodyLabel;

    [Header("Optional CTA")]
    [Tooltip("Activated only when the entry has a buttonLabel + windowPrefabToOpen.")]
    [SerializeField] private GameObject ctaContainer;
    [SerializeField] private Button ctaButton;
    [SerializeField] private TMP_Text ctaLabel;

    public void Set(MessageEntry entry)
    {
        if (entry == null) return;
        if (senderLabel != null) senderLabel.text = entry.sender;
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
    }
}
