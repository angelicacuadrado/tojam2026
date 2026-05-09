using TMPro;
using UnityEngine;

public class CreditsWindow : MonoBehaviour
{
    [SerializeField] private TextAsset creditsAsset;
    [SerializeField] private TMP_Text bodyLabel;

    private void OnEnable()
    {
        if (bodyLabel == null) return;
        bodyLabel.text = creditsAsset != null ? creditsAsset.text : "(credits text not assigned)";
    }
}
