using UnityEngine;

public class ConsoleInteraction : MonoBehaviour
{
    public GameObject passwordPanel;

    private bool playerNear = false;

    void Update()
    {
        if (playerNear && Input.GetKeyDown(KeyCode.E))
        {
            passwordPanel.SetActive(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
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