using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
public class SimpleDraggableWindow : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private bool DragInProgress = false;
    private bool canDrag = false;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
    }


    public void OnPointerDown(PointerEventData eventData)
    {

        // Check if we can drag based on the layer
        canDrag = IsOnDraggableLayer(eventData.pointerEnter);

        if (canDrag)
        {
            MouseCameraMovement.instance.freezeMovement = true;
            DragInProgress = true;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!canDrag)
            return;

        Vector2 moveVector = eventData.delta;

        // Adjust for canvas scale factor
        if (parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            moveVector /= parentCanvas.scaleFactor;
        }
        // Move this window
        rectTransform.anchoredPosition += moveVector;
        // Move all siblings as well
        MoveSiblings(moveVector);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        MouseCameraMovement.instance.freezeMovement = false;
        DragInProgress = false;
        canDrag = false;
    }

    private void MoveSiblings(Vector2 moveVector)
    {
        Transform parent = rectTransform.parent;
        List<RectTransform> children = parent.GetComponentsInChildren<RectTransform>().ToList();


        foreach (RectTransform child in children)
        {
            if(child != rectTransform)
            {
                child.anchoredPosition += moveVector;
            }
        }
    }

    private bool IsOnDraggableLayer(GameObject obj)
    {
        if (obj == null)
        {
            return false;
        }
        else
        {
            return obj.layer == LayerMask.NameToLayer("UI_Draggable");
        }
    }
}