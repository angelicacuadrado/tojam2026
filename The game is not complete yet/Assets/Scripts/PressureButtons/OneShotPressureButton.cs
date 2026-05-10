using UnityEngine;

public class OneShotPressureButton : PressureButtonBase
{
    private bool hasBeenPressed;

    private void OnTriggerEnter(Collider other)
    {
        if (hasBeenPressed || !IsValidPressureSource(other))
            return;

        hasBeenPressed = true;
        SetPressed(true);
    }
}
