using UnityEngine;
using UnityEngine.EventSystems;

//[RequireComponent(typeof(WindowControls))]
public class WindowFocus : MonoBehaviour, IPointerDownHandler
{
    private WindowControls controls;

    private void Awake()
    {
        controls = GetComponent<WindowControls>();
        if (controls == null)
        {
            controls = GetComponentInParent<WindowControls>();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (WindowManager.Instance == null) return;
        WindowManager.Instance.BringToFront(controls);
       
    }
}
