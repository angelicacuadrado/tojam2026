using UnityEngine;

/// <summary>
/// Bridges level-completion events to the message scheduler.
/// Each Exit.ExitLevel that triggers AnyLevelCompleted advances one chapter.
/// </summary>
public class ChapterProgressManager : MonoBehaviour
{
    [Tooltip("If left empty, uses MessageScheduler.Instance.")]
    [SerializeField] private MessageScheduler scheduler;

    private void OnEnable()
    {
        Exit.AnyLevelCompleted += OnLevelCompleted;
    }

    private void OnDisable()
    {
        Exit.AnyLevelCompleted -= OnLevelCompleted;
    }

    private void OnLevelCompleted(Exit _)
    {
        var s = scheduler != null ? scheduler : MessageScheduler.Instance;
        if (s == null)
        {
            Debug.LogWarning("[ChapterProgressManager] No MessageScheduler available; cannot advance chapter.");
            return;
        }
        s.AdvanceToNextChapter();
    }
}
