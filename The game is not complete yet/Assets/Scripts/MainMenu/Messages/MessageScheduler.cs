using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MessageScheduler : MonoBehaviour
{
    public static MessageScheduler Instance { get; private set; }

    [SerializeField] private MessageStory story;
    [Tooltip("Seconds to wait after scene start before Chapter 1 begins delivering.")]
    [SerializeField] private float startupDelay = 2f;
    [SerializeField] private bool autoStart = true;

    [SerializeField] private CallWindow callWindow;

    [Header("Audio Settings")]
    [Tooltip("Sound name for regular notifications (when chat is closed/unfocused).")]
    [SerializeField] private string notificationSfxName = "newmessage";

    [Tooltip("Sound name to play when a message arrives WHILE looking at the chat (soft pop).")]

    /// <summary>
    /// Set true while the user is actively reading Dev's chat — the scheduler then
    /// skips the notification SFX (notifications are meant for unattended messages).
    /// </summary>
    public bool SuppressNotificationSound { get; set; }

    public event Action<MessageEntry> MessageDelivered;
    public event Action<string> TimeMarkerDelivered;
    public event Action<int> ChapterStarted;
    public event Action<int> ChapterCompleted;

    public enum FeedItemKind { Message, TimeMarker }

    [Serializable]
    public struct FeedItem
    {
        public FeedItemKind Kind;
        public MessageEntry Message;
        public string TimeLabel;
    }

    private readonly List<MessageEntry> delivered = new();
    private readonly List<FeedItem> deliveredItems = new();

    public IReadOnlyList<MessageEntry> Delivered => delivered;
    public IReadOnlyList<FeedItem> DeliveredItems => deliveredItems;

    public int CurrentChapterIndex => currentChapterIndex;
    public bool IsPlayingChapter => playingChapter;

    private int currentChapterIndex = -1;
    private bool playingChapter;
    private bool startupDone;
    private Coroutine runner;
    private bool advanceQueued;

    public CallWindow CallWindow => callWindow;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(startupDelay);
        startupDone = true;
        if (autoStart) AdvanceToNextChapter();
    }

    public void AdvanceToNextChapter()
    {
        if (!startupDone)
        {
            StartCoroutine(WaitThenAdvance());
            return;
        }

        if (playingChapter)
        {
            advanceQueued = true;
            return;
        }

        if (story == null || story.chapters == null) return;

        int next = currentChapterIndex + 1;
        if (next >= story.chapters.Count) return;

        currentChapterIndex = next;
        runner = StartCoroutine(PlayChapter(story.chapters[next], next));
    }

    private IEnumerator WaitThenAdvance()
    {
        while (!startupDone) yield return null;
        AdvanceToNextChapter();
    }

    private IEnumerator PlayChapter(MessageChapter chapter, int index)
    {
        playingChapter = true;
        ChapterStarted?.Invoke(index);

        if (chapter != null)
        {
            if (!string.IsNullOrEmpty(chapter.startTimeLabel))
            {
                deliveredItems.Add(new FeedItem { Kind = FeedItemKind.TimeMarker, TimeLabel = chapter.startTimeLabel });
                TimeMarkerDelivered?.Invoke(chapter.startTimeLabel);
            }

            if (chapter.messages != null)
            {
                foreach (var entry in chapter.messages)
                {
                    if (entry == null) continue;

                    if (entry.delayAfterPrevious > 0f)
                        yield return new WaitForSecondsRealtime(entry.delayAfterPrevious);

                    delivered.Add(entry);
                    deliveredItems.Add(new FeedItem { Kind = FeedItemKind.Message, Message = entry });
                    MessageDelivered?.Invoke(entry);

                    if (AudioManager.Instance != null)
                    {
                        if (!SuppressNotificationSound && !string.IsNullOrEmpty(notificationSfxName))
                        {
                            AudioManager.Instance.PlaySFX(notificationSfxName);
                        }
                        else if (SuppressNotificationSound && !string.IsNullOrEmpty("Message"))
                        {
                            AudioManager.Instance.PlaySFX("Message");
                        }
                    }
                }
            }
        }

        playingChapter = false;
        runner = null;
        ChapterCompleted?.Invoke(index);

        if (advanceQueued)
        {
            advanceQueued = false;
            AdvanceToNextChapter();
        }
    }
}