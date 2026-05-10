using UnityEngine;

public class DoorAudio : MonoBehaviour
{
    private bool hasPlayed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!hasPlayed)
        {
            hasPlayed = true;

            if (NarratorController.Instance != null)
            {
                NarratorController.Instance.PlayLine("Level3_3");
            }
            else
            {
                Debug.LogWarning("Door_audio: No NarratorController found in the scene!");
            }
        }
    }
}
