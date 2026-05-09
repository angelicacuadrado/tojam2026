using System.Collections;
using UnityEngine;

public class NarratorController : MonoBehaviour
{
    public static NarratorController Instance { get; private set; }

    [System.Serializable]
    public struct VoiceLine
    {
        [Tooltip("Unique ID to trigger this line via code (e.g., 'PickedUpKey')")]
        public string id;
        public float delayBeforePlaying;
        public AudioClip clip;
        [TextArea(2, 4)] public string subtitle;
    }

    [Header("Level Intro Sequence")]
    [SerializeField, Tooltip("Plays automatically when the level starts.")]
    private VoiceLine[] startSequence;

    [Header("Event-Triggered Lines")]
    [SerializeField, Tooltip("Lines played only when triggered by game events.")]
    private VoiceLine[] eventLines;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (startSequence != null && startSequence.Length > 0)
        {
            StartCoroutine(PlaySequence(startSequence));
        }
    }

    /// <summary>
    /// Call this from ANY script to play a specific narrator line!
    /// Example: NarratorController.Instance.PlayLine("PlayerHurt");
    /// </summary>
    public void PlayLine(string lineId)
    {
        foreach (var line in eventLines)
        {
            if (line.id == lineId)
            {
                StartCoroutine(PlaySingleLine(line));
                return;
            }
        }
        Debug.LogWarning($"Narrator: Could not find voice line with ID '{lineId}'");
    }

    private IEnumerator PlaySingleLine(VoiceLine line)
    {
        if (line.delayBeforePlaying > 0f) yield return new WaitForSeconds(line.delayBeforePlaying);

        if (AudioManager.Instance != null && line.clip != null)
            AudioManager.Instance.PlayVoice(line.clip);

        if (!string.IsNullOrEmpty(line.subtitle))
            Debug.Log($"Narrator Subtitle: {line.subtitle}"); // Link to UI later
    }

    private IEnumerator PlaySequence(VoiceLine[] sequence)
    {
        foreach (var line in sequence)
        {
            yield return StartCoroutine(PlaySingleLine(line));
            if (line.clip != null) yield return new WaitForSeconds(line.clip.length);
        }
    }
}