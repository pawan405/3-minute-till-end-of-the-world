using UnityEngine;

public class PanelTrigger : MonoBehaviour
{
    public GameObject panel;

    private bool playerNear = false;

    private void Start()
    {
        panel.SetActive(false);
    }

    private void Update()
    {
        if (playerNear && Input.GetKeyDown(KeyCode.E))
        {
            panel.SetActive(!panel.activeSelf);
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
        if (other.CompareTag("Player"))
        {
            playerNear = false;
            panel.SetActive(false);
        }
    }
}