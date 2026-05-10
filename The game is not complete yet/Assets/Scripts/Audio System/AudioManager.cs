using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // Required for the new mouse click logic

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
    public List<SoundEntry> sounds = new List<SoundEntry>();
    private Dictionary<string, AudioClip> soundDictionary = new Dictionary<string, AudioClip>();

    [Header("Mouse Clicks (Random)")]
    [Tooltip("Drag your various mouse click sounds here.")]
    public List<AudioClip> randomClickSounds = new List<AudioClip>();

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource voiceSource;
    [SerializeField, Tooltip("Dedicated source for mouse clicks")]
    private AudioSource clickSource;

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
                soundDictionary.Add(sound.name, sound.clip);
        }
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            PlayRandomClick();
        }
    }

    private void PlayRandomClick()
    {
        if (randomClickSounds.Count == 0 || clickSource == null) return;
        int randomIndex = Random.Range(0, randomClickSounds.Count);
        clickSource.PlayOneShot(randomClickSounds[randomIndex]);
    }

    public void PlaySFX(string soundName, float volumeScale = 1f)
    {
        if (soundDictionary.TryGetValue(soundName, out AudioClip clip))
        {
            sfxSource.PlayOneShot(clip, volumeScale);
        }
    }

    public void PlayBGM(string soundName, bool loop = true)
    {
        if (soundDictionary.TryGetValue(soundName, out AudioClip clip))
        {
            bgmSource.Stop();
            bgmSource.clip = clip;
            bgmSource.loop = loop;
            bgmSource.Play();
        }
    }

    public void PlayVoice(AudioClip clip)
    {
        if (clip == null || voiceSource == null) return;

        voiceSource.Stop();
        voiceSource.clip = clip;
        voiceSource.Play();
    }

    public void Play3DSFX(string soundName, Vector3 position)
    {
        if (soundDictionary.TryGetValue(soundName, out AudioClip clip))
        {
            AudioSource.PlayClipAtPoint(clip, position);
        }
        else
        {
            Debug.LogWarning($"AudioManager: Sound '{soundName}' not found!");
        }
    }

    public void PauseBGM()
    {
         bgmSource.Pause();
    }

    public void UnPauseBGM()
    {
        bgmSource.UnPause();
    }
}