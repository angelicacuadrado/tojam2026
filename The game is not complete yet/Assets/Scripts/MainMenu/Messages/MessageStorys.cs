using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "TOJam/Message Story", fileName = "MessageStory")]
public class MessageStory : ScriptableObject
{
    [Tooltip("Ordered list of chapters. ChapterProgressManager advances one chapter per level completion.")]
    public List<MessageChapter> chapters = new();
}