using ithappy.Creative_Characters_FREE.Controller;
using UnityEngine;
using UnityEngine.InputSystem;

public class BreakerInteraction : MonoBehaviour
{
    [SerializeField] private GameObject breakerCanvas;
    [SerializeField] private GameObject breakerPrompt;

    [SerializeField] private CharacterMover characterMover;
    [SerializeField] private MovePlayerInput movePlayerInput;

    [SerializeField] private BreakerPuzzle breakerPuzzle;

    private bool playerNearby = false;
    private bool panelOpen = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (breakerPuzzle.PuzzleSolved)
            return;

        playerNearby = true;
        breakerPrompt.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerNearby = false;
        breakerPrompt.SetActive(false);
    }

    private void Update()
    {
        if (!playerNearby)
            return;

        if (!panelOpen && Keyboard.current.eKey.wasPressedThisFrame)
        {
            breakerCanvas.SetActive(true);
            breakerPrompt.SetActive(false);

            characterMover.enabled = false;
            movePlayerInput.enabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            panelOpen = true;
        }

        if (panelOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            breakerCanvas.SetActive(false);

            characterMover.enabled = true;
            movePlayerInput.enabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            panelOpen = false;

            if (!breakerPuzzle.PuzzleSolved)
                breakerPrompt.SetActive(true);
        }
    }
}