using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsWindow : MonoBehaviour
{
    [Header("Volume")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Text volumeValueLabel;

    [Header("Mouse Sensitivity")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private TMP_Text sensitivityValueLabel;

    private void OnEnable()
    {
        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.SetValueWithoutNotify(SettingsManager.Volume);
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = SettingsManager.MinSensitivity;
            sensitivitySlider.maxValue = SettingsManager.MaxSensitivity;
            sensitivitySlider.SetValueWithoutNotify(SettingsManager.MouseSensitivity);
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }

        RefreshLabels();
        SettingsManager.Changed += RefreshLabels;
    }

    private void OnDisable()
    {
        if (volumeSlider != null) volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        if (sensitivitySlider != null) sensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);
        SettingsManager.Changed -= RefreshLabels;
    }

    private void OnVolumeChanged(float v) => SettingsManager.Volume = v;
    private void OnSensitivityChanged(float v) => SettingsManager.MouseSensitivity = v;

    private void RefreshLabels()
    {
        if (volumeValueLabel != null) volumeValueLabel.text = Mathf.RoundToInt(SettingsManager.Volume * 100f) + "%";
        if (sensitivityValueLabel != null) sensitivityValueLabel.text = SettingsManager.MouseSensitivity.ToString("0.0");
    }
}
