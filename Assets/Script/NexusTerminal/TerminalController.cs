using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TerminalController : MonoBehaviour
{
    private const string Prompt = "nexus@station:~$";
    private const string UnknownCommandPrefix = "COMMAND NOT FOUND: ";

    [Header("Dependencies")]
    [SerializeField] private TerminalUI terminalUI;
    [SerializeField] private CommandDatabase commandDatabase;

    [Header("Progression")]
    [SerializeField] private LevelFlowController levelFlow;

    [Header("Authentication")]
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private GameObject passwordPanel;
    [SerializeField] private GameObject terminalPanel;
    [SerializeField] private TMP_Text authenticationStatusText;
    [SerializeField] private string expectedPassword = "1234";
    [SerializeField] private bool focusPasswordOnStart = true;

    [Header("Backgrounds")]
    [SerializeField] private Image terminalBackdrop;
    [SerializeField] private Sprite passwordBackgroundSprite;
    [SerializeField] private Sprite terminalBackgroundSprite;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float authenticationPause = 0.45f;
    [SerializeField, Min(0f)] private float bootLinePause = 0.35f;
    [SerializeField, Min(0f)] private float commandPause = 0.15f;

    [Header("Signal Acquisition")]
    [SerializeField] private Transform signalBarFrame;
    [SerializeField] private TMP_Text signalValue;
    [SerializeField, Min(0.01f)] private float signalStepDuration = 0.16f;
    [SerializeField, Min(0.1f)] private float signalUpdateInterval = 0.24f;
    [SerializeField] private Color signalInactiveColor = new Color(0.04f, 0.2f, 0.12f, 0.2f);
    [SerializeField] private Color signalActiveColor = new Color(0.15f, 1f, 0.52f, 0.9f);

    private bool authenticated;
    private bool commandInProgress;
    private Coroutine terminalRoutine;
    private Coroutine signalRoutine;
    private Image[] signalBars = System.Array.Empty<Image>();
    private bool signalAcquisitionComplete;

    public TMP_InputField CommandInput => terminalUI != null ? terminalUI.CommandInput : null;
    public bool IsAuthenticated => authenticated;
    public bool IsInputEnabled => terminalUI != null && terminalUI.IsInputEnabled;

    private void Awake()
    {
        if (passwordInput != null)
        {
            passwordInput.lineType = TMP_InputField.LineType.SingleLine;
            passwordInput.contentType = TMP_InputField.ContentType.Password;
            passwordInput.onSubmit.AddListener(OnPasswordSubmitted);
        }

        if (terminalUI != null && terminalUI.CommandInput != null)
        {
            terminalUI.CommandInput.lineType = TMP_InputField.LineType.SingleLine;
            terminalUI.CommandInput.onSubmit.AddListener(OnCommandSubmitted);
        }
    }

    private void OnEnable()
    {
        ResetSession();
    }

    private void Start()
    {
        ResetSession();

        if (focusPasswordOnStart && passwordInput != null)
        {
            passwordInput.Select();
            passwordInput.ActivateInputField();
        }
    }

    private void ResetSession()
    {
        authenticated = false;
        commandInProgress = false;
        signalAcquisitionComplete = false;

        if (terminalRoutine != null)
        {
            StopCoroutine(terminalRoutine);
            terminalRoutine = null;
        }

        if (signalRoutine != null)
        {
            StopCoroutine(signalRoutine);
            signalRoutine = null;
        }

        CacheSignalDisplay();
        SetSignalLevel(0, 0f);
        SetTerminalBackground(passwordBackgroundSprite);

        if (passwordPanel != null)
        {
            passwordPanel.SetActive(true);
        }

        if (terminalPanel != null)
        {
            terminalPanel.SetActive(false);
        }

        if (authenticationStatusText != null)
        {
            authenticationStatusText.text = string.Empty;
        }

        if (passwordInput != null)
        {
            passwordInput.text = string.Empty;
        }

        if (terminalUI != null)
        {
            terminalUI.ResetTerminal();
        }
    }

    private void SetTerminalBackground(Sprite backgroundSprite)
    {
        if (terminalBackdrop == null || backgroundSprite == null)
        {
            return;
        }

        terminalBackdrop.sprite = backgroundSprite;
        terminalBackdrop.color = Color.white;
        terminalBackdrop.type = Image.Type.Simple;
        terminalBackdrop.preserveAspect = false;
    }

    private void BeginSignalAcquisition()
    {
        if (signalRoutine != null)
        {
            return;
        }

        if (!CacheSignalDisplay())
        {
            signalAcquisitionComplete = true;
            return;
        }

        signalAcquisitionComplete = false;
        signalRoutine = StartCoroutine(SignalAcquisitionRoutine());
    }

    private bool CacheSignalDisplay()
    {
        if (signalBarFrame == null && passwordPanel != null)
        {
            signalBarFrame = passwordPanel.transform.Find("SignalBarFrame");
        }

        if (signalValue == null && passwordPanel != null)
        {
            signalValue = passwordPanel.transform.Find("SignalValue")?.GetComponent<TMP_Text>();
        }

        if (signalBarFrame == null || signalBarFrame.childCount == 0)
        {
            return false;
        }

        signalBars = new Image[signalBarFrame.childCount];
        for (int i = 0; i < signalBars.Length; i++)
        {
            signalBars[i] = signalBarFrame.GetChild(i).GetComponent<Image>();
        }

        return true;
    }

    private IEnumerator SignalAcquisitionRoutine()
    {
        SetSignalLevel(0, 0f);

        for (int i = 0; i < signalBars.Length; i++)
        {
            float stepVariation = 0.78f + Mathf.Abs(Mathf.Sin(i * 1.71f)) * 0.42f;
            yield return new WaitForSecondsRealtime(signalStepDuration * stepVariation);

            SetSignalLevel(i + 1, i * 0.37f);

            if (signalValue != null)
            {
                int percentage = Mathf.RoundToInt((i + 1) * 100f / signalBars.Length);
                signalValue.text = $"ACQUIRING {percentage:00}%";
            }
        }

        signalAcquisitionComplete = true;

        if (signalValue != null)
        {
            signalValue.text = "ENCRYPTED";
        }

        float elapsed = 0f;
        while (passwordPanel == null || passwordPanel.activeSelf)
        {
            elapsed += signalUpdateInterval;
            float signalNoise = Mathf.PerlinNoise(elapsed * 0.8f, 0.31f);
            int dropoutCount = signalNoise > 0.96f ? 2 : signalNoise > 0.82f ? 1 : 0;
            int litBars = Mathf.Max(signalBars.Length - dropoutCount, 1);

            SetSignalLevel(litBars, elapsed);
            yield return new WaitForSecondsRealtime(signalUpdateInterval);
        }

        signalRoutine = null;
    }

    private void SetSignalLevel(int litBars, float phase)
    {
        for (int i = 0; i < signalBars.Length; i++)
        {
            Image bar = signalBars[i];
            if (bar == null)
            {
                continue;
            }

            if (i >= litBars)
            {
                bar.color = signalInactiveColor;
                continue;
            }

            float shimmer = Mathf.PerlinNoise(phase * 0.9f + i * 0.17f, 0.7f) * 0.18f;
            bar.color = Color.Lerp(signalActiveColor, signalInactiveColor, shimmer);
        }
    }

    private void OnDestroy()
    {
        if (passwordInput != null)
        {
            passwordInput.onSubmit.RemoveListener(OnPasswordSubmitted);
        }

        if (terminalUI != null && terminalUI.CommandInput != null)
        {
            terminalUI.CommandInput.onSubmit.RemoveListener(OnCommandSubmitted);
        }
    }

    /// <summary>
    /// Public button-compatible authentication entry point.
    /// </summary>
    public void CheckPassword()
    {
        if (passwordInput != null)
        {
            Authenticate(passwordInput.text);
        }
    }

    /// <summary>
    /// Public button-compatible command submission entry point.
    /// </summary>
    public void SubmitCommand()
    {
        if (terminalUI != null && terminalUI.CommandInput != null)
        {
            OnCommandSubmitted(terminalUI.CommandInput.text);
        }
    }

    /// <summary>
    /// Assigns the authoritative progression controller at runtime or from setup code.
    /// </summary>
    public void SetLevelFlow(LevelFlowController flowController)
    {
        levelFlow = flowController;
    }

    private void OnPasswordSubmitted(string submittedPassword)
    {
        Authenticate(submittedPassword);
    }

    private void Authenticate(string submittedPassword)
    {
        if (authenticated || terminalRoutine != null)
        {
            return;
        }

        if (string.Equals(submittedPassword.Trim(), expectedPassword.Trim(), System.StringComparison.Ordinal))
        {
            BeginSignalAcquisition();
            terminalRoutine = StartCoroutine(BootTerminalRoutine());
            return;
        }

        if (authenticationStatusText != null)
        {
            authenticationStatusText.text = "ACCESS DENIED\nWRONG PASSWORD\n\nPLEASE TRY AGAIN";
        }

        if (passwordInput != null)
        {
            passwordInput.text = string.Empty;
            passwordInput.Select();
            passwordInput.ActivateInputField();
        }
    }

    private IEnumerator BootTerminalRoutine()
    {
        authenticated = true;

        if (authenticationStatusText != null)
        {
            authenticationStatusText.text = "VERIFYING CREDENTIALS...";
        }

        yield return new WaitForSecondsRealtime(authenticationPause);

        while (!signalAcquisitionComplete)
        {
            yield return null;
        }

        if (passwordPanel != null)
        {
            passwordPanel.SetActive(false);
        }

        SetTerminalBackground(terminalBackgroundSprite);

        if (terminalPanel != null)
        {
            terminalPanel.SetActive(true);
        }

        if (terminalUI == null)
        {
            terminalRoutine = null;
            yield break;
        }

        terminalUI.ResetTerminal();
        terminalUI.SetPromptVisible(false);

        yield return terminalUI.TypeText("NEXUS SPACE SYSTEMS\n\n", false);
        yield return new WaitForSecondsRealtime(bootLinePause);
        yield return terminalUI.TypeText("NEXUS TERMINAL v2.4\n\n", false);
        yield return new WaitForSecondsRealtime(bootLinePause);
        yield return terminalUI.TypeText("Initializing secure session...\n\n", false);
        yield return new WaitForSecondsRealtime(bootLinePause);
        yield return terminalUI.TypeText("Loading command interface...", true);
        yield return new WaitForSecondsRealtime(bootLinePause);

        terminalUI.SetPrompt(Prompt);
        terminalUI.SetCommandInputEnabled(true);
        terminalUI.FocusCommandInput();
        terminalRoutine = null;
    }

    private void OnCommandSubmitted(string rawInput)
    {
        if (!authenticated || commandInProgress || terminalUI == null)
        {
            return;
        }

        string commandText = rawInput == null ? string.Empty : rawInput.Trim();
        terminalUI.CommandInput.text = string.Empty;

        if (string.IsNullOrWhiteSpace(commandText))
        {
            terminalUI.FocusCommandInput();
            return;
        }

        StartCoroutine(ProcessCommandRoutine(commandText));
    }

    private IEnumerator ProcessCommandRoutine(string commandText)
    {
        commandInProgress = true;
        terminalUI.AppendCommandLine(commandText);
        yield return new WaitForSecondsRealtime(commandPause);

        if (levelFlow != null && levelFlow.TryHandleTerminalCommand(commandText, out string progressionResponse))
        {
            if (!string.IsNullOrWhiteSpace(progressionResponse))
            {
                yield return terminalUI.TypeText(progressionResponse, true);
            }
        }
        else
        {
            string commandName = CommandDatabase.GetCommandName(commandText);
            if (commandName == "clear")
            {
                terminalUI.ClearHistory();
            }
            else if (commandName == "help")
            {
                yield return terminalUI.TypeText(commandDatabase.BuildHelpResponse(), true);
            }
            else if (commandDatabase.TryGetCommand(commandText, out CommandDatabase.CommandDefinition definition))
            {
                yield return terminalUI.TypeText(definition.response, true);
            }
            else
            {
                yield return terminalUI.TypeText(
                    $"{UnknownCommandPrefix}{commandName}\nTYPE 'help' FOR AVAILABLE COMMANDS.",
                    true);
            }
        }

        bool flowOwnsInput = levelFlow != null &&
            (levelFlow.CurrentState == LevelFlowController.LevelState.REACTOR_ACCESS ||
             levelFlow.CurrentState == LevelFlowController.LevelState.LAUNCHING ||
             levelFlow.CurrentState == LevelFlowController.LevelState.SPACE_SHOOTER);

        if (!flowOwnsInput)
        {
            terminalUI.SetPrompt(Prompt);
            terminalUI.SetCommandInputEnabled(true);
            terminalUI.FocusCommandInput();
        }

        commandInProgress = false;
    }
}
