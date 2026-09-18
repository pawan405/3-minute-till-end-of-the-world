using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class NexusTerminalInteraction : MonoBehaviour
{
    [Header("Terminal")]
    [SerializeField] private GameObject terminalRoot;
    [SerializeField] private TMP_Text interactionPrompt;
    [SerializeField] private GameObject interactionPromptBackdrop;
    [SerializeField] private string interactionKeyLabel = "E";

    [Header("Interaction")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool hideCursorWhenClosed = true;

    private bool playerInRange;

    private void Awake()
    {
        SetPromptVisible(false);
        SetTerminalVisible(false);
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(interactionKey))
        {
            OpenTerminal();
        }

        if (terminalRoot != null && terminalRoot.activeSelf && Input.GetKeyDown(closeKey))
        {
            CloseTerminal();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playerInRange = true;
        SetPromptVisible(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playerInRange = false;
        SetPromptVisible(false);
    }

    /// <summary>
    /// Opens the NEXUS terminal when the player is inside the trigger volume.
    /// </summary>
    public void OpenTerminal()
    {
        if (!playerInRange || terminalRoot == null)
        {
            return;
        }

        terminalRoot.SetActive(true);
        SetPromptVisible(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Closes the NEXUS terminal and returns control to the player.
    /// </summary>
    public void CloseTerminal()
    {
        if (terminalRoot != null)
        {
            terminalRoot.SetActive(false);
        }

        if (hideCursorWhenClosed)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (playerInRange)
        {
            SetPromptVisible(true);
        }
    }

    private void SetTerminalVisible(bool visible)
    {
        if (terminalRoot != null && terminalRoot.activeSelf != visible)
        {
            terminalRoot.SetActive(visible);
        }
    }

    private void SetPromptVisible(bool visible)
    {
        if (interactionPromptBackdrop != null)
        {
            interactionPromptBackdrop.SetActive(visible);
        }

        if (interactionPrompt == null)
        {
            return;
        }

        interactionPrompt.text = visible
            ? $"PRESS [ {interactionKeyLabel} ] TO ACCESS NEXUS TERMINAL"
            : string.Empty;
        interactionPrompt.gameObject.SetActive(visible);
    }
}
