using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class DesktopIcon : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject windowPrefab;
    [Tooltip("If true, requires a double click to open (XP-style). Otherwise single click opens.")]
    [SerializeField] private bool doubleClickToOpen = true;
    [SerializeField] private float doubleClickWindow = 0.4f;

    private float lastClickTime = -1f;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!doubleClickToOpen)
        {
            Open();
            return;
        }

        if (Time.unscaledTime - lastClickTime <= doubleClickWindow)
        {
            lastClickTime = -1f;
            Open();
        }
        else
        {
            lastClickTime = Time.unscaledTime;
        }
    }

    public void Open()
    {
        if (windowPrefab == null || WindowManager.Instance == null) return;
        WindowManager.Instance.Open(windowPrefab);
    }
}
