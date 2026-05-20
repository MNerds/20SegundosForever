using UnityEngine;
using UnityEngine.EventSystems;

public class SnapScrollDragForwarder : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public SnapScrollRect snapScrollRect;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (snapScrollRect != null)
            snapScrollRect.BeginSwipe(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (snapScrollRect != null)
            snapScrollRect.DragSwipe(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (snapScrollRect != null)
            snapScrollRect.EndSwipe();
    }
}