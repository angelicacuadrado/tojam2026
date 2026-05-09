using System;
using UnityEngine;

public static class SettingsManager
{
    private const string VolumeKey = "settings.volume";
    private const string SensitivityKey = "settings.mouseSensitivity";

    public const float DefaultVolume = 0.8f;
    public const float DefaultMouseSensitivity = 2f;

    public const float MinSensitivity = 0.1f;
    public const float MaxSensitivity = 10f;

    public static event Action Changed;

    private static float volume = DefaultVolume;
    private static float mouseSensitivity = DefaultMouseSensitivity;
    private static bool initialized;

    public static float Volume
    {
        get { EnsureInit(); return volume; }
        set
        {
            EnsureInit();
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(clamped, volume)) return;
            volume = clamped;
            PlayerPrefs.SetFloat(VolumeKey, volume);
            PlayerPrefs.Save();
            ApplyVolume();
            Changed?.Invoke();
        }
    }

    public static float MouseSensitivity
    {
        get { EnsureInit(); return mouseSensitivity; }
        set
        {
            EnsureInit();
            float clamped = Mathf.Clamp(value, MinSensitivity, MaxSensitivity);
            if (Mathf.Approximately(clamped, mouseSensitivity)) return;
            mouseSensitivity = clamped;
            PlayerPrefs.SetFloat(SensitivityKey, mouseSensitivity);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() => EnsureInit();

    private static void EnsureInit()
    {
        if (initialized) return;
        initialized = true;
        volume = PlayerPrefs.GetFloat(VolumeKey, DefaultVolume);
        mouseSensitivity = PlayerPrefs.GetFloat(SensitivityKey, DefaultMouseSensitivity);
        ApplyVolume();
    }

    private static void ApplyVolume()
    {
        AudioListener.volume = volume;
    }
}
