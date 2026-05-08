using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MessageScheduler : MonoBehaviour
{
    public static MessageScheduler Instance { get; private set; }

    [SerializeField] private MessageConversation conversation;
    [Tooltip("Seconds to wait after scene start before the first message in the list ticks down.")]
    [SerializeField] private float startupDelay = 2f;
    [SerializeField] private bool autoStart = true;

    public event Action<MessageEntry> MessageDelivered;

    private readonly List<MessageEntry> delivered = new();
    public IReadOnlyList<MessageEntry> Delivered => delivered;

    private Coroutine runner;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void Start()
    {
        if (autoStart) StartConversation();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void StartConversation()
    {
        if (runner != null || conversation == null || conversation.messages == null) return;
        runner = StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        if (startupDelay > 0f) yield return new WaitForSeconds(startupDelay);

        foreach (var entry in conversation.messages)
        {
            if (entry == null) continue;
            if (entry.delayAfterPrevious > 0f) yield return new WaitForSeconds(entry.delayAfterPrevious);

            delivered.Add(entry);
            MessageDelivered?.Invoke(entry);
        }
        runner = null;
    }
}
