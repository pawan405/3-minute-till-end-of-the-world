using System.Collections;
using ithappy.Creative_Characters_FREE.Controller;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Connects the electricity panel trigger to the electrical circuit puzzle UI.
/// </summary>
public class ElectricityPanelInteraction : MonoBehaviour
{
    [Header("Interaction UI")]
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private GameObject onlinePrompt;

    [Header("Puzzle")]
    [SerializeField] private GameObject puzzleUI;
    [SerializeField] private ElectricalCircuitPuzzle electricalCircuitPuzzle;

    [Header("Player Control")]
    [SerializeField] private CharacterMover characterMover;
    [SerializeField] private MovePlayerInput movePlayerInput;

    [Header("Online Message")]
    [SerializeField] private float onlineMessageDuration = 3f;

    private const string PlayerTag = "Player";

    private bool playerNearby;
    private bool puzzleOpen;
    private bool systemOnline;
    private Coroutine onlineMessageCoroutine;

    private void Start()
    {
        SetPromptActive(interactionPrompt, false);
        SetPromptActive(onlinePrompt, false);
        SetPuzzleActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(PlayerTag) || systemOnline)
            return;

        playerNearby = true;

        if (!puzzleOpen)
            SetPromptActive(interactionPrompt, true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(PlayerTag))
            return;

        playerNearby = false;

        if (!puzzleOpen)
            SetPromptActive(interactionPrompt, false);
    }

    private void Update()
    {
        if (systemOnline)
            return;

        if (puzzleOpen)
        {
            if (electricalCircuitPuzzle != null && electricalCircuitPuzzle.PuzzleSolved)
            {
                CompletePuzzle();
                return;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                ClosePuzzle();

            return;
        }

        if (playerNearby && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            OpenPuzzle();
    }

    private void OpenPuzzle()
    {
        if (systemOnline || electricalCircuitPuzzle == null)
            return;

        puzzleOpen = true;
        SetPromptActive(interactionPrompt, false);
        SetPuzzleActive(true);
        SetPlayerControl(false);
        SetCursorForPuzzle(true);
    }

    private void ClosePuzzle()
    {
        puzzleOpen = false;
        SetPuzzleActive(false);
        SetPlayerControl(true);
        SetCursorForPuzzle(false);

        if (playerNearby)
            SetPromptActive(interactionPrompt, true);
    }

    private void CompletePuzzle()
    {
        systemOnline = true;
        puzzleOpen = false;
        SetPuzzleActive(false);
        SetPromptActive(interactionPrompt, false);
        SetPlayerControl(true);
        SetCursorForPuzzle(false);

        SetPromptActive(onlinePrompt, true);

        if (onlineMessageCoroutine != null)
            StopCoroutine(onlineMessageCoroutine);

        onlineMessageCoroutine = StartCoroutine(HideOnlinePromptAfterDelay());
    }

    private IEnumerator HideOnlinePromptAfterDelay()
    {
        yield return new WaitForSeconds(onlineMessageDuration);
        SetPromptActive(onlinePrompt, false);
        onlineMessageCoroutine = null;
    }

    private void SetPuzzleActive(bool isActive)
    {
        if (puzzleUI != null)
            puzzleUI.SetActive(isActive);
    }

    private void SetPromptActive(GameObject prompt, bool isActive)
    {
        if (prompt != null)
            prompt.SetActive(isActive);
    }

    private void SetPlayerControl(bool canMove)
    {
        if (characterMover != null)
        {
            characterMover.enabled = canMove;
            characterMover.canMove = canMove;
        }

        if (movePlayerInput != null)
            movePlayerInput.enabled = canMove;
    }

    private void SetCursorForPuzzle(bool puzzleIsOpen)
    {
        Cursor.lockState = puzzleIsOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = puzzleIsOpen;
    }
}
