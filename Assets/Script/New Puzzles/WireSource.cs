using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class WireSource : MonoBehaviour , IPointerDownHandler , IDragHandler , IEndDragHandler
//IPointerDownHandler -> ui object wants to know when the object clicks on it
{
    private bool isConnected;
    public WirePuzzleManager puzzleManager;
    public enum WireColor
    {
        Red,
        Green,
        Blue,
        Yellow
    }
    public WireColor wireColor;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isConnected)
            return;
        Debug.Log("Red SOurce Clicked");
        puzzleManager.StartWire(this); //tells the manager that player have grabbed the wire so start making it 
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (isConnected)
            return;
        Debug.Log("Red Source Is Dragged");
        puzzleManager.UpdateWire(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isConnected)
            return;
        puzzleManager.EndWire(eventData.position);
    }
    public void SetConnected()
    {
        isConnected = true;
    }
}
