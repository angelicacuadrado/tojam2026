using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "TOJam/Message Chapter", fileName = "MessageChapter")]
public class MessageChapter : ScriptableObject
{
    [Tooltip("Optional time-stamp shown as a centered marker before this chapter's first message (e.g., \"10:30 AM\"). Leave blank to skip.")]
    public string startTimeLabel;
    public List<MessageEntry> messages = new();
}