using UnityEngine;
using UnityEngine.EventSystems;

 public enum ToolType
    {
        Alert,
        Release,
        Ambulance
    }
public class DraggableTool : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
   

    [Header("Tool Info")]
    public ToolType toolType;

    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform dragVisual;
    private CanvasGroup dragVisualCanvasGroup;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        GameObject dragObject = Instantiate(gameObject, canvas.transform);
        dragObject.name = gameObject.name + "_DragVisual";

        DraggableTool duplicateScript = dragObject.GetComponent<DraggableTool>();
        if (duplicateScript != null)
        {
            Destroy(duplicateScript);
        }

        dragVisual = dragObject.GetComponent<RectTransform>();
        dragVisual.position = rectTransform.position;
        dragVisual.SetAsLastSibling();

        dragVisualCanvasGroup = dragObject.GetComponent<CanvasGroup>();
        if (dragVisualCanvasGroup == null)
        {
            dragVisualCanvasGroup = dragObject.AddComponent<CanvasGroup>();
        }

        dragVisualCanvasGroup.blocksRaycasts = false;
        dragVisualCanvasGroup.alpha = 0.8f;

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 1f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragVisual != null)
        {
            dragVisual.position += (Vector3)eventData.delta;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        if (dragVisual != null)
        {
            Destroy(dragVisual.gameObject);
            dragVisual = null;
            dragVisualCanvasGroup = null;
        }
    }
}