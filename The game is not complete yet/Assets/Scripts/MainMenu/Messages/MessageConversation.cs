using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MessageEntry
{
    public string sender = "Friend";
    [TextArea(1, 4)] public string body;
    [Tooltip("Seconds to wait after the previous message (or after scheduler start, for the first one) before delivery.")]
    public float delayAfterPrevious = 5f;
}

[CreateAssetMenu(menuName = "TOJam/Message Conversation", fileName = "MessageConversation")]
public class MessageConversation : ScriptableObject
{
    public List<MessageEntry> messages = new();
}
