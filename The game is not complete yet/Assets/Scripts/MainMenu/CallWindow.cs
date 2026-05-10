using System.Collections;
using TMPro;
using UnityEngine;

public class CallWindow : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;

    [Header("Incoming")]
    [SerializeField] private string incomingText = "Incoming...";
    [SerializeField] private Color incomingColor = Color.black;
    [SerializeField] private float incomingDuration = 1f;
    [Tooltip("AudioManager SFX key played when 'Incoming...' appears.")]
    [SerializeField] private string incomingSfxName = "callstart";

    [Header("Connected")]
    [SerializeField] private string connectedText = "Connected";
    [SerializeField] private Color connectedColor = Color.green;

    [Header("Ended")]
    [Tooltip("Wait this long after StartEnding() before showing the red 'Ended Call' label.")]
    [SerializeField] private float endingDelay = 3f;
    [SerializeField] private string endedText = "Ended Call";
    [SerializeField] private Color endedColor = Color.red;
    [SerializeField] private float endedDuration = 1f;
    [Tooltip("AudioManager SFX key played when 'Ended Call' appears (call hang-up).")]

    private Coroutine _routine;

    /// <summary>Activate the window, show "Incoming..." for the configured duration, then "Connected".</summary>
    public void StartIncoming()
    {
        gameObject.SetActive(true);
        StopActive();
        _routine = StartCoroutine(IncomingRoutine());
    }

    /// <summary>Wait `endingDelay`, show "Ended Call" for `endedDuration`, then deactivate.</summary>
    public void StartEnding()
    {
        if (!gameObject.activeInHierarchy) return;
        StopActive();
        _routine = StartCoroutine(EndingRoutine());
    }

    /// <summary>Stops any pending sequence and hides immediately.</summary>
    public void HideImmediate()
    {
        StopActive();
        gameObject.SetActive(false);
    }

    public void UpdateMessageText(string message, Color color) => SetText(message, color);

    private void StopActive()
    {
        if (_routine != null) { StopCoroutine(_routine); _routine = null; }
    }

    private IEnumerator IncomingRoutine()
    {
        SetText(incomingText, incomingColor);
        PlaySfx();
        yield return new WaitForSecondsRealtime(incomingDuration);
        SetText(connectedText, connectedColor);
        _routine = null;
    }

    private IEnumerator EndingRoutine()
    {
        yield return new WaitForSecondsRealtime(endingDelay);
        SetText(endedText, endedColor);
        PlaySfx();
        yield return new WaitForSecondsRealtime(endedDuration);
        gameObject.SetActive(false);
        _routine = null;
    }

    private static void PlaySfx()
    {
        if (string.IsNullOrEmpty("Call")) return;
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.PlaySFX("Call");
    }

    private void SetText(string text, Color color)
    {
        if (messageText == null) return;
        messageText.text = text;
        messageText.color = color;
    }
}
