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

    [Tooltip("Sound name in AudioManager's database to play when a new message arrives.")]
    [SerializeField] private string notificationSfxName = "newmessage";

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

    /// <summary>Messages-only history (in delivery order). Use <see cref="DeliveredItems"/> when you need time markers too.</summary>
    public IReadOnlyList<MessageEntry> Delivered => delivered;

    /// <summary>Full ordered feed including chapter time markers — use this when rebuilding the chat view from scratch.</summary>
    public IReadOnlyList<FeedItem> DeliveredItems => deliveredItems;

    public int CurrentChapterIndex => currentChapterIndex;
    public bool IsPlayingChapter => playingChapter;

    private int currentChapterIndex = -1;
    private bool playingChapter;
    private bool startupDone;
    private bool advanceQueued;
    private Coroutine runner;

    /// <summary>Optional CallWindow used by LevelHost to play the incoming/ending UI around level runs.</summary>
    public CallWindow CallWindow => callWindow;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (autoStart) StartCoroutine(InitialDelay());
    }

    private IEnumerator InitialDelay()
    {
        if (startupDelay > 0f) yield return new WaitForSecondsRealtime(startupDelay);
        startupDone = true;
        AdvanceToNextChapter();
    }

    /// <summary>
    /// Advances to the next chapter and starts delivering its messages.
    /// No-op if already mid-chapter, or no more chapters left.
    /// </summary>
    public void AdvanceToNextChapter()
    {
        if (!startupDone)
        {
            // Allow external triggers before startupDelay completes by deferring.
            StartCoroutine(WaitThenAdvance());
            return;
        }
        if (playingChapter)
        {
            // Level completed before current chapter finished delivering — queue the
            // advance so it runs as soon as PlayChapter wraps up.
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
                    if (entry.delayAfterPrevious > 0f) yield return new WaitForSecondsRealtime(entry.delayAfterPrevious);
                    delivered.Add(entry);
                    deliveredItems.Add(new FeedItem { Kind = FeedItemKind.Message, Message = entry });
                    MessageDelivered?.Invoke(entry);
                    if (!SuppressNotificationSound && AudioManager.Instance != null && !string.IsNullOrEmpty(notificationSfxName))
                    {
                        AudioManager.Instance.PlaySFX(notificationSfxName);
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
