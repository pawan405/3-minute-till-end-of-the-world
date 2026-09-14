using UnityEngine;

public class ConsoleInteraction : MonoBehaviour
{
    public GameObject passwordPanel;

    private bool playerNear = false;


    void Update()
    {
        // Open Panel
        if (playerNear && Input.GetKeyDown(KeyCode.E))
        {
            passwordPanel.SetActive(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }


        // Close Panel
        if (passwordPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            passwordPanel.SetActive(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNear = true;
        }
    }


    private void OnTriggerExit(Collider other)
    {
        playerNear = false;
    }
}