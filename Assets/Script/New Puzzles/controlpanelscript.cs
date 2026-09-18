using UnityEngine;
using UnityEngine.InputSystem;

public class controlpanelscript : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private GameObject interactionPrompt;

    [Header("Generator")]
    [SerializeField] private GeneratorController generatorController;

    private bool playerNearby = false;

    private void Start()
    {
        interactionPrompt.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (generatorController.IsPuzzleSolved())
            return;

        playerNearby = true;
        interactionPrompt.SetActive(true);

        Debug.Log("Player is near control panel");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerNearby = false;
        interactionPrompt.SetActive(false);

        Debug.Log("Player left control panel");
    }

    private void Update()
    {
        if (!playerNearby)
            return;

        if (generatorController.IsPuzzleSolved())
            return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            interactionPrompt.SetActive(false);

            generatorController.OpenPuzzle();

            playerNearby = false;
        }
    }
}
