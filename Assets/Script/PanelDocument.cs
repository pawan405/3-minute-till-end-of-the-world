

using UnityEngine;

public class PanelDocument : MonoBehaviour
{
    public GameObject documentUI;

    bool playerNear = false;


    void Start()
    {

        documentUI.SetActive(false);
    }


    void Update()
    {
        if (playerNear && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("OPENING IMAGE");
            OpenDocument();
        }


        if (documentUI.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseDocument();
        }
    }


    void OpenDocument()
    {
        documentUI.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }


    void CloseDocument()
    {
        documentUI.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNear = true;
            Debug.Log("Near Panel");
        }
    }


    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNear = false;
        }
    }
}