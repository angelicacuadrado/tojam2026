using UnityEngine;
using UnityEngine.EventSystems;

public class StartMenuBackdrop : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private StartMenuController controller;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (controller != null) controller.SetMenuVisible(false);
    }
}
