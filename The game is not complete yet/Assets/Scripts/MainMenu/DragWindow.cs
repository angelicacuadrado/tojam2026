using UnityEngine;
using UnityEngine.EventSystems;

public class DragWindow : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    private Vector2 offset;
    private RectTransform window;
    
    private void Awake()
    {
        window = transform.parent.GetComponent<RectTransform>(); 
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(window, eventData.position, eventData.pressEventCamera, out offset);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 pos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(window.parent as RectTransform, eventData.position,
                eventData.pressEventCamera, out pos))
        {
            window.anchoredPosition = pos - offset;
        }
    }
}
