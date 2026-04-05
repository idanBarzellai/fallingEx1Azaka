using UnityEngine;
using UnityEngine.EventSystems;

public class SectorDropZone : MonoBehaviour, IDropHandler
{
    public enum SectorName
    {
        Eilat,
        South,
        Center,
        Sharon,
        North,
    }

    public SectorName sectorName;

   private SectorHandler sectorHandler;

    private void Awake()
    {
        sectorHandler = GetComponent<SectorHandler>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        DraggableTool tool = eventData.pointerDrag?.GetComponent<DraggableTool>();

        if (tool != null && sectorHandler != null)
        {
            Debug.Log(tool.toolType + " dropped on " + sectorHandler.sectorName);
            sectorHandler.HandleTool(tool.toolType);
        }
    }
}