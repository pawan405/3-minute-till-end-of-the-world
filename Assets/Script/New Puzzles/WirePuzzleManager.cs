using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class WirePuzzleManager : MonoBehaviour
{
    public GameObject wirePrefab; // wirePrefab from puzzlePrefab
    public RectTransform wireContainer; //every genrated wire will live inside it 
    private GameObject currentWire;// which wire we are currently dragging 
    private RectTransform currentWireRect;// access to wire's rect transform
    private RectTransform currentSourceRect;
    public bool isDragging; // tels if the wire is being dragged

    public void StartWire(WireSource source)
    {
        currentWire = Instantiate(wirePrefab, wireContainer);

        currentWireRect = currentWire.GetComponent<RectTransform>();

        currentSourceRect = source.GetComponent<RectTransform>();

        Image wireImage = currentWire.GetComponent<Image>();

        if (wireImage != null)
        {
            switch (source.wireColor)
            {
                case WireSource.WireColor.Red:
                    wireImage.color = Color.red;
                    break;

                case WireSource.WireColor.Blue:
                    wireImage.color = Color.blue;
                    break;

                case WireSource.WireColor.Yellow:
                    wireImage.color = Color.yellow;
                    break;

                case WireSource.WireColor.Green:
                    wireImage.color = Color.green;
                    break;
            }
        }

        isDragging = true;
    }

    public void UpdateWire(Vector2 mousePosition)
    {
        Vector2 startPosition = wireContainer.InverseTransformPoint(
            currentSourceRect.position
        );

        Vector2 mouseLocalPosition;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            wireContainer,
            mousePosition,
            null,
            out mouseLocalPosition
        );

        Vector2 direction = mouseLocalPosition - startPosition;

        currentWireRect.anchoredPosition = startPosition;

        currentWireRect.sizeDelta = new Vector2(
            direction.magnitude,
            10f
        );

        float angle = Mathf.Atan2(
            direction.y,
            direction.x
        ) * Mathf.Rad2Deg;

        currentWireRect.localRotation = Quaternion.Euler(
            0f,
            0f,
            angle
        );
    }

    public void EndWire(Vector2 mousePosition)
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();

        EventSystem.current.RaycastAll(pointerData, results);

        WireTarget target = null;

        foreach (RaycastResult result in results)
        {
            target = result.gameObject.GetComponent<WireTarget>();

            if (target != null)
            {
                break;
            }
        }

        bool correctConnection = false;

        if (target != null)
        {
            WireSource source = currentSourceRect.GetComponent<WireSource>();

            if (target.wireColor == source.wireColor)
            {
                correctConnection = true;
                source.SetConnected();

                Debug.Log("CORRECT CONNECTION!");
            }
            else
            {
                Debug.Log("WRONG TARGET!");
            }
        }
        else
        {
            Debug.Log("NO TARGET!");
        }

        if (!correctConnection)
        {
            Destroy(currentWire);
        }

        currentWire = null;
        currentWireRect = null;
        currentSourceRect = null;
        isDragging = false;
    }

    //public void EndWire(Vector2 mousePosition)
    //{
    //    if (currentWire != null)
    //    {
    //        Destroy(currentWire);
    //    }

    //    currentWire = null;
    //    currentWireRect = null;
    //    currentSourceRect = null;
    //    isDragging = false;
    //}

    //public void UpdateWire(Vector2 mousePosition)
    //{
    //    Vector2 startPosition = wireContainer.InverseTransformPoint(
    //        currentSourceRect.position
    //    );
    //    Vector2 mouseLocalPosition;

    //    RectTransformUtility.ScreenPointToLocalPointInRectangle(
    //        wireContainer,
    //        mousePosition,
    //        null,
    //        out mouseLocalPosition
    //        );
    //    Vector2 direction = mouseLocalPosition - startPosition;
    //    currentWireRect.anchoredPosition = mouseLocalPosition;

    //    currentWireRect.sizeDelta = new Vector2(
    //        direction.magnitude,
    //        currentWireRect.sizeDelta.y
    //    );

    //    float angle = Mathf.Atan2(
    //        direction.y,
    //        direction.x
    //        )*Mathf.Rad2Deg;

    //    currentWireRect.localRotation = Quaternion.Euler(0, 0, angle);
    //}



}
