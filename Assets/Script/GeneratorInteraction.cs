using ithappy.Creative_Characters_FREE.Controller;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class GeneratorInteraction : MonoBehaviour
{
    [SerializeField] private GameObject generatorCanvas;
    [SerializeField] private GameObject generatorPrompt;

    [SerializeField] private CharacterMover characterMover;
    [SerializeField] private MovePlayerInput movePlayerInput;
    [SerializeField] private GeneratorPuzzle generatorPuzzle;

    private bool playerNearby = false;
    private bool panelOpen = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (generatorPuzzle.PuzzleSolved)
            return;

        playerNearby = true;
        generatorPrompt.SetActive(true);
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player Left");

            playerNearby = false;
            generatorPrompt.SetActive(false);
        }
    }
    //private void Update()
    //{
    //    if (!playerNearby)
    //        return;
    //    if (Keyboard.current.eKey.wasPressedThisFrame)
    //    {
    //        generatorCanvas.SetActive(true);
    //        generatorPrompt.SetActive(false);

    //        characterMover.enabled = false;
    //        movePlayerInput.enabled = false;

    //        Cursor.lockState = CursorLockMode.None;
    //        Cursor.visible = true;

    //        panelOpen = true;
    //    }
    //}
    private void Update()
    {
        if (generatorPuzzle.PuzzleSolved)
        {
            generatorPrompt.SetActive(false);

            if (!panelOpen)
            {
                playerNearby = false;
            }
        }
        if (!playerNearby)
            return;

        if (!panelOpen)
        {
            if (generatorPuzzle.PuzzleSolved)
                return;
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                generatorCanvas.SetActive(true);
                generatorPrompt.SetActive(false);

                characterMover.enabled = false;
                movePlayerInput.enabled = false;

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                panelOpen = true;
            }
        }

        if (panelOpen)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                generatorCanvas.SetActive(false);

                characterMover.enabled = true;
                movePlayerInput.enabled = true;

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                panelOpen = false;

                generatorPrompt.SetActive(true);
            }
        }
    }
}