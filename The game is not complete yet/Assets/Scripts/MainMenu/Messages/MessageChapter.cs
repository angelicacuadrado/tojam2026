using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "TOJam/Message Chapter", fileName = "MessageChapter")]
public class MessageChapter : ScriptableObject
{
    public List<MessageEntry> messages = new();
}