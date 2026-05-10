using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class WeightedPressureButton : PressureButtonBase
{
    [Header("Release Event")]
    [SerializeField] private UnityEvent onReleased;
    private static bool isPressed;
    private readonly HashSet<Collider> pressureSources = new();

    private void OnTriggerEnter(Collider other)
    {
        if (!IsValidPressureSource(other))
            return;

        pressureSources.Add(other);
        if (NarratorController.Instance != null && !isPressed)
        {
            NarratorController.Instance.PlayLine("Level3_2");
            isPressed = true;
        }
        SetPressed(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!pressureSources.Remove(other))
            return;

        RemoveMissingSources();
        SetPressed(pressureSources.Count > 0);
    }

    private void OnDisable()
    {
        pressureSources.Clear();
        SetPressed(false);
    }

    private void RemoveMissingSources()
    {
        pressureSources.RemoveWhere(source => source == null || !source.gameObject.activeInHierarchy);
    }

    protected override void OnReleased()
    {
        base.OnReleased();
        onReleased?.Invoke();
    }
}
