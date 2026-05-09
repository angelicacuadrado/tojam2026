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

    public event Action<MessageEntry> MessageDelivered;
    public event Action<int> ChapterStarted;
    public event Action<int> ChapterCompleted;

    private readonly List<MessageEntry> delivered = new();
    public IReadOnlyList<MessageEntry> Delivered => delivered;

    public int CurrentChapterIndex => currentChapterIndex;
    public bool IsPlayingChapter => playingChapter;

    private int currentChapterIndex = -1;
    private bool playingChapter;
    private bool startupDone;
    private Coroutine runner;

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
        if (startupDelay > 0f) yield return new WaitForSeconds(startupDelay);
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
        if (playingChapter || story == null || story.chapters == null) return;

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

        if (chapter != null && chapter.messages != null)
        {
            foreach (var entry in chapter.messages)
            {
                if (entry == null) continue;
                if (entry.delayAfterPrevious > 0f) yield return new WaitForSeconds(entry.delayAfterPrevious);
                delivered.Add(entry);
                MessageDelivered?.Invoke(entry);
            }
        }

        playingChapter = false;
        runner = null;
        ChapterCompleted?.Invoke(index);
    }
}
