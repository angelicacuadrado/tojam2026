using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    [System.Serializable]
    public struct SoundEntry
    {
        public string name;
        public AudioClip clip;
    }

    [Header("Audio Database")]
    [Tooltip("SFX and Voice clips")]
    public List<SoundEntry> sounds = new List<SoundEntry>();
    private Dictionary<string, AudioClip> soundDictionary = new Dictionary<string, AudioClip>();

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource voiceSource;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        foreach (var sound in sounds)
        {
            if (!soundDictionary.ContainsKey(sound.name) && sound.clip != null)
            {
                soundDictionary.Add(sound.name, sound.clip);
            }
        }
    }

    /// <summary>
    /// Plays a sound effect by name. Sounds can overlap.
    /// </summary>
    public void PlaySFX(string soundName)
    {
        if (soundDictionary.TryGetValue(soundName, out AudioClip clip))
        {
            sfxSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning($"AudioManager: Sound '{soundName}' not found in the database!");
        }
    }

    /// <summary>
    /// Plays a voice line by name. Stops the current voice so they don't overlap.
    /// </summary>
    public void PlayVoice(string soundName)
    {
        if (soundDictionary.TryGetValue(soundName, out AudioClip clip))
        {
            voiceSource.Stop();
            voiceSource.clip = clip;
            voiceSource.Play();
        }
        else
        {
            Debug.LogWarning($"AudioManager: Voice '{soundName}' not found in the database!");
        }
    }

    /// <summary>
    /// Changes the background music.
    /// </summary>
    public void PlayMusic(AudioClip clip)
    {
        if (bgmSource.clip == clip) return;

        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.Play();
    }
}