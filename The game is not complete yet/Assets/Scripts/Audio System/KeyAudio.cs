using UnityEngine;

public class KeyAudio : MonoBehaviour
{
    private bool hasPlayed = false;
    private void OnCollisionEnter(Collision collision)
    {
        if (!hasPlayed && collision.gameObject.CompareTag("Player"))
        {
            hasPlayed = true;
            if (NarratorController.Instance != null)
            {
                NarratorController.Instance.PlayLine("Level3_1");
            }
            else
            {
                Debug.LogWarning("Key_audio: No NarratorController found in the scene!");
            }
        }
    }
}
