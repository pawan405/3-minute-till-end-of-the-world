using System;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ReactorAccess : MonoBehaviour
{
    private const string ReactorClue = "REACTOR SYSTEM\n\nCORE STATUS: UNSTABLE\n\nCOOLING SYSTEM: FAILING\n\nROCKET LINK: LOCKED\n\nAUTHORIZATION: REQUIRED\n\nEMERGENCY ROCKET REQUIRES REACTOR AUTHORIZATION.\nACCESS REACTOR FILE TO CONTINUE.";
    private const string AccessSuccess = "REACTOR FILE ACCESSED\n\n--------------------------------\n\nPROJECT: NEXUS-01\nTYPE: EMERGENCY ESCAPE VEHICLE\n\nDESTINATION: ORBITAL SAFE ZONE\n\nLAUNCH STATUS: READY\nREACTOR AUTHORIZATION: ACCEPTED\n\nLAUNCH AUTHORIZATION GRANTED";

    [Header("Dependencies")]
    [SerializeField] private LevelFlowController levelFlow;
    [SerializeField] private TerminalUI terminalUI;

    [Header("Access Panel")]
    [SerializeField] private GameObject accessPanel;
    [SerializeField] private TMP_Text panelStatusText;
    [SerializeField] private TMP_InputField accessCodeInput;
    [SerializeField] private string requiredAccessCode = "7319";
    [SerializeField] private string accessPrompt = "ENTER REACTOR ACCESS CODE";

    private bool reactorFileAccessed;
    private bool reactorAuthorizationAccepted;
    private bool listenersRegistered;

    public bool ReactorFileAccessed => reactorFileAccessed;
    public bool ReactorAuthorizationAccepted => reactorAuthorizationAccepted;

    private void Awake()
    {
        RegisterInputListener();
        SetPanelVisible(false);
    }

    private void OnDestroy()
    {
        if (listenersRegistered && accessCodeInput != null)
        {
            accessCodeInput.onSubmit.RemoveListener(OnAccessCodeSubmitted);
        }
    }

    /// <summary>
    /// Handles the reactor clue and opens the compact code panel when requested.
    /// </summary>
    public bool TryBeginAccess(string rawInput, out string response)
    {
        response = string.Empty;
        string command = rawInput == null ? string.Empty : rawInput.Trim();

        if (string.Equals(command, "reactor", StringComparison.OrdinalIgnoreCase))
        {
            response = ReactorClue;
            return true;
        }

        if (!string.Equals(command, "reactor access", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (reactorAuthorizationAccepted)
        {
            response = AccessSuccess;
            return true;
        }

        OpenAccessPanel();
        response = "REACTOR ACCESS PANEL OPEN\nENTER CODE TO ACCESS THE NEXUS FILE.";
        return true;
    }

    /// <summary>
    /// Validates a submitted reactor code and records both authorization flags on success.
    /// </summary>
    public bool TrySubmitAccessCode(string rawInput, out string response)
    {
        response = string.Empty;
        if (levelFlow == null || levelFlow.CurrentState != LevelFlowController.LevelState.REACTOR_ACCESS)
        {
            response = "REACTOR ACCESS IS NOT ACTIVE";
            return false;
        }

        string submittedCode = rawInput == null ? string.Empty : rawInput.Trim();
        if (string.IsNullOrWhiteSpace(submittedCode) || !string.Equals(submittedCode, requiredAccessCode.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            response = "ACCESS CODE INVALID\nPLEASE TRY AGAIN";
            SetPanelStatus(response);
            ClearAndFocusInput();
            return true;
        }

        if (reactorAuthorizationAccepted)
        {
            response = AccessSuccess;
            return true;
        }

        reactorFileAccessed = true;
        reactorAuthorizationAccepted = true;
        levelFlow.SetReactorAuthorizationAccepted();
        SetPanelVisible(false);
        response = AccessSuccess;
        return true;
    }

    /// <summary>
    /// Clears the access flags and closes the panel.
    /// </summary>
    public void ResetAccess()
    {
        reactorFileAccessed = false;
        reactorAuthorizationAccepted = false;
        SetPanelVisible(false);
    }

    /// <summary>
    /// Submits the visible access-code input from a UI button.
    /// </summary>
    public void SubmitAccessCode()
    {
        if (accessCodeInput == null)
        {
            return;
        }

        OnAccessCodeSubmitted(accessCodeInput.text);
    }

    private void RegisterInputListener()
    {
        if (accessCodeInput == null || listenersRegistered)
        {
            return;
        }

        accessCodeInput.lineType = TMP_InputField.LineType.SingleLine;
        accessCodeInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        accessCodeInput.onSubmit.AddListener(OnAccessCodeSubmitted);
        listenersRegistered = true;
    }

    private void OnAccessCodeSubmitted(string submittedCode)
    {
        if (!TrySubmitAccessCode(submittedCode, out string response))
        {
            return;
        }

        if (terminalUI != null)
        {
            terminalUI.AppendInstant(response + "\n");
            terminalUI.SetPrompt("nexus@station:~$");
            terminalUI.SetCommandInputEnabled(levelFlow != null && levelFlow.CurrentState == LevelFlowController.LevelState.LAUNCH_READY);
            terminalUI.FocusCommandInput();
        }
    }

    private void OpenAccessPanel()
    {
        if (terminalUI != null)
        {
            terminalUI.SetCommandInputEnabled(true);
            terminalUI.SetPrompt("CODE:");
            terminalUI.SetPromptVisible(true);
        }

        SetPanelVisible(true);
        if (accessPanel != null)
        {
            accessPanel.transform.SetAsLastSibling();
            Canvas panelCanvas = accessPanel.GetComponent<Canvas>();
            if (panelCanvas == null)
            {
                panelCanvas = accessPanel.AddComponent<Canvas>();
            }

            panelCanvas.overrideSorting = true;
            panelCanvas.sortingOrder = 1000;
            panelCanvas.enabled = true;
        }

        SetPanelStatus(accessPrompt);
        ClearAndFocusInput();
        if (terminalUI != null)
        {
            terminalUI.FocusCommandInput();
        }
    }

    private void SetPanelVisible(bool visible)
    {
        if (accessPanel != null)
        {
            accessPanel.SetActive(visible);
        }
    }

    private void SetPanelStatus(string status)
    {
        if (panelStatusText != null)
        {
            panelStatusText.text = status;
        }
    }

    private void ClearAndFocusInput()
    {
        if (accessCodeInput == null)
        {
            return;
        }

        accessCodeInput.text = string.Empty;
        accessCodeInput.Select();
        accessCodeInput.ActivateInputField();
    }
}
