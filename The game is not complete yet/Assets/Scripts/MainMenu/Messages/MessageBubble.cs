using TMPro;
using UnityEngine;

public class MessageBubble : MonoBehaviour
{
    [SerializeField] private TMP_Text senderLabel;
    [SerializeField] private TMP_Text bodyLabel;

    public void Set(MessageEntry entry)
    {
        if (entry == null) return;
        if (senderLabel != null) senderLabel.text = entry.sender;
        if (bodyLabel != null) bodyLabel.text = entry.body;
    }
}
