using UnityEngine;

public class NarratorController : MonoBehaviour
{
    public static NarratorController Instance { get; private set; }

    [SerializeField,Tooltip("Audio clips for the narrator")]
    private AudioClip[] narratorAudioClips;
    [SerializeField, Tooltip("Subtitles corresponding to the narrator audio clips")]
    private string[] subtitleText;
}