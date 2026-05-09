using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewAudioLibrary", menuName = "Audio/Audio Library")]
public class AudioLibrary : ScriptableObject
{
    [System.Serializable]
    public struct SoundEffect
    {
        public string name;
        public AudioClip clip;
    }

    public List<SoundEffect> sfxList;
    public AudioClip GetClip(string soundName)
    {
        var sound = sfxList.Find(s => s.name == soundName);
        if (sound.clip == null) Debug.LogWarning($"Sound '{soundName}' not found in library!");
        return sound.clip;
    }
}