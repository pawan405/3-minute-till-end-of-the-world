using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Builds an interactive six-digit keypad and launches the configured minigame after valid progression.
/// </summary>
public sealed class FinalCodeTerminal : MonoBehaviour
{
    private const float CanvasReferenceWidth = 1920f;
    private const float CanvasReferenceHeight = 1080f;
    private const float CanvasScaleMatch = 0.5f;
    private const int CanvasSortingOrder = 100;
    private const int MaximumCodeLength = 6;
    private const float PanelWidth = 600f;
    private const float PanelHeight = 740f;
    private const float TitlePositionY = 315f;
    private const float TitleWidth = 500f;
    private const float TitleHeight = 44f;
    private const float DisplayPositionY = 245f;
    private const float DisplayWidth = 500f;
    private const float DisplayHeight = 68f;
    private const float StatusPositionY = 180f;
    private const float StatusWidth = 520f;
    private const float StatusHeight = 48f;
    private const float ButtonWidth = 145f;
    private const float ButtonHeight = 62f;
    private const float ButtonColumnOffset = 165f;
    private const float ButtonRow1Y = 95f;
    private const float ButtonRow2Y = 15f;
    private const float ButtonRow3Y = -65f;
    private const float ButtonRow4Y = -145f;
    private const float SubmitButtonPositionY = -255f;
    private const float SubmitButtonWidth = 450f;
    private const float SubmitButtonHeight = 64f;
    private const float CloseButtonPositionX = 250f;
    private const float CloseButtonPositionY = 320f;
    private const float CloseButtonSize = 42f;
    private const float BackdropAlpha = 0.76f;
    private const float PanelCornerAlpha = 0.98f;
    private const string LockedMessage = "ACCESS LOCKED — COMPLETE ALL PUZZLES";
    private const string DeniedMessage = "ACCESS DENIED";
    private const string GrantedMessage = "ACCESS GRANTED";
    private const string ReadyMessage = "ENTER SIX-DIGIT ACCESS CODE";
    private const string KeypadTitle = "FINAL ACCESS TERMINAL";
    private static readonly Color BackdropColor = new Color(0.005f, 0.018f, 0.035f, BackdropAlpha);
    private static readonly Color PanelColor = new Color(0.012f, 0.035f, 0.07f, PanelCornerAlpha);
    private static readonly Color ButtonColor = new Color(0.035f, 0.12f, 0.19f, 1f);
    private static readonly Color AccentColor = new Color(0.16f, 0.72f, 0.94f, 1f);
    private static readonly Color TextColor = new Color(0.82f, 0.95f, 1f, 1f);
    private static readonly Color ErrorColor = new Color(1f, 0.28f, 0.24f, 1f);
    private static readonly Color SuccessColor = new Color(0.28f, 1f, 0.72f, 1f);

    [Header("Progression and Launch")]
    [SerializeField] private PuzzleProgressionManager progressionManager;
    [SerializeField] private LevelTransition levelTransition;
    [SerializeField] private UnityEvent onAccessGranted = new UnityEvent();
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;

    private readonly StringBuilder enteredCode = new StringBuilder(MaximumCodeLength);
    private GameObject generatedCanvasRoot;
    private GameObject keypadPanel;
    private TextMeshProUGUI inputDisplay;
    private TextMeshProUGUI statusDisplay;
    private NexusTerminalInteraction terminalInteraction;
    private bool terminalOpen;
    private bool launchTriggered;

    private void Awake()
    {
        terminalInteraction = GetComponent<NexusTerminalInteraction>();
        BuildKeypadUI();
        generatedCanvasRoot.SetActive(false);
    }

    private void Update()
    {
        if (terminalOpen && Input.GetKeyDown(closeKey))
        {
            CloseTerminal();
        }
    }

    /// <summary>Opens the keypad and prevents launching until all three puzzle fragments are collected.</summary>
    public void OpenTerminal()
    {
        if (launchTriggered)
        {
            return;
        }

        if (progressionManager == null)
        {
            SetStatus(LockedMessage, ErrorColor);
        }
        else
        {
            SetStatus(progressionManager.AllPuzzlesCompleted ? ReadyMessage : LockedMessage,
                progressionManager.AllPuzzlesCompleted ? AccentColor : ErrorColor);
        }

        enteredCode.Clear();
        RefreshInputDisplay();
        generatedCanvasRoot.SetActive(true);
        terminalOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>Closes the keypad and restores the first-person cursor state.</summary>
    public void CloseTerminal()
    {
        if (!terminalOpen)
        {
            return;
        }

        terminalOpen = false;
        generatedCanvasRoot.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (!launchTriggered && terminalInteraction != null)
        {
            terminalInteraction.OnFinalCodeTerminalClosed();
        }
    }

    /// <summary>Appends one digit while keeping the entered code at six digits maximum.</summary>
    public void EnterDigit(string digit)
    {
        if (!terminalOpen || enteredCode.Length >= MaximumCodeLength || string.IsNullOrEmpty(digit)
            || digit.Length != 1 || digit[0] < '0' || digit[0] > '9')
        {
            return;
        }

        enteredCode.Append(digit);
        RefreshInputDisplay();
    }

    /// <summary>Clears every digit currently entered on the keypad.</summary>
    public void ClearInput()
    {
        if (!terminalOpen)
        {
            return;
        }

        enteredCode.Clear();
        RefreshInputDisplay();
        SetStatus(progressionManager != null && progressionManager.AllPuzzlesCompleted ? ReadyMessage : LockedMessage,
            progressionManager != null && progressionManager.AllPuzzlesCompleted ? AccentColor : ErrorColor);
    }

    /// <summary>Removes the most recently entered digit.</summary>
    public void BackspaceInput()
    {
        if (!terminalOpen || enteredCode.Length == 0)
        {
            return;
        }

        enteredCode.Length -= 1;
        RefreshInputDisplay();
    }

    /// <summary>Validates the entered code and launches the configured minigame at most once.</summary>
    public void SubmitCode()
    {
        if (!terminalOpen || launchTriggered)
        {
            return;
        }

        if (progressionManager == null || !progressionManager.AllPuzzlesCompleted)
        {
            SetStatus(LockedMessage, ErrorColor);
            return;
        }

        if (!progressionManager.IsFinalAccessCodeCorrect(enteredCode.ToString()))
        {
            SetStatus(DeniedMessage, ErrorColor);
            enteredCode.Clear();
            RefreshInputDisplay();
            return;
        }

        launchTriggered = true;
        SetStatus(GrantedMessage, SuccessColor);
        terminalOpen = false;
        generatedCanvasRoot.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (levelTransition != null)
        {
            levelTransition.LoadSpaceShooter();
        }
        else if (onAccessGranted != null && onAccessGranted.GetPersistentEventCount() > 0)
        {
            onAccessGranted.Invoke();
        }
        else
        {
            Debug.LogError("Final code accepted, but no direct minigame scene transition is configured.", this);
        }
    }

    private void BuildKeypadUI()
    {
        generatedCanvasRoot = new GameObject(
            "FinalCodeKeypadCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = generatedCanvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CanvasSortingOrder;

        CanvasScaler canvasScaler = generatedCanvasRoot.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(CanvasReferenceWidth, CanvasReferenceHeight);
        canvasScaler.matchWidthOrHeight = CanvasScaleMatch;

        GameObject backdrop = CreateImage("KeypadBackdrop", generatedCanvasRoot.transform, Vector2.zero, BackdropColor);
        RectTransform backdropRect = backdrop.GetComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.pivot = new Vector2(0.5f, 0.5f);
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;

        keypadPanel = CreateImage("KeypadPanel", generatedCanvasRoot.transform, new Vector2(PanelWidth, PanelHeight), PanelColor);
        CreateText("TerminalTitle", keypadPanel.transform, KeypadTitle, new Vector2(0f, TitlePositionY), new Vector2(TitleWidth, TitleHeight), 28, AccentColor, TextAlignmentOptions.Center, true);
        inputDisplay = CreateText("CodeDisplay", keypadPanel.transform, "______", new Vector2(0f, DisplayPositionY), new Vector2(DisplayWidth, DisplayHeight), 38, TextColor, TextAlignmentOptions.Center, true);
        statusDisplay = CreateText("StatusDisplay", keypadPanel.transform, ReadyMessage, new Vector2(0f, StatusPositionY), new Vector2(StatusWidth, StatusHeight), 18, AccentColor, TextAlignmentOptions.Center, true);

        float[] columnPositions = { -ButtonColumnOffset, 0f, ButtonColumnOffset };
        float[] rowPositions = { ButtonRow1Y, ButtonRow2Y, ButtonRow3Y };
        int digit = 1;
        for (int row = 0; row < rowPositions.Length; row++)
        {
            for (int column = 0; column < columnPositions.Length; column++)
            {
                string value = digit.ToString();
                CreateButton(value, new Vector2(columnPositions[column], rowPositions[row]), new Vector2(ButtonWidth, ButtonHeight), () => EnterDigit(value));
                digit++;
            }
        }

        CreateButton("BACKSPACE", new Vector2(columnPositions[0], ButtonRow4Y), new Vector2(ButtonWidth, ButtonHeight), BackspaceInput);
        CreateButton("0", new Vector2(columnPositions[1], ButtonRow4Y), new Vector2(ButtonWidth, ButtonHeight), () => EnterDigit("0"));
        CreateButton("CLEAR", new Vector2(columnPositions[2], ButtonRow4Y), new Vector2(ButtonWidth, ButtonHeight), ClearInput);
        CreateButton("SUBMIT", new Vector2(0f, SubmitButtonPositionY), new Vector2(SubmitButtonWidth, SubmitButtonHeight), SubmitCode, AccentColor);
        CreateButton("X", new Vector2(CloseButtonPositionX, CloseButtonPositionY), new Vector2(CloseButtonSize, CloseButtonSize), CloseTerminal, ButtonColor);
    }

    private GameObject CreateImage(string objectName, Transform parent, Vector2 size, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return imageObject;
    }

    private TextMeshProUGUI CreateText(string objectName, Transform parent, string initialText, Vector2 position, Vector2 size, float fontSize, Color color, TextAlignmentOptions alignment, bool bold)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = initialText;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        text.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }

        return text;
    }

    private void CreateButton(string label, Vector2 position, Vector2 size, UnityAction onClick, Color? colorOverride = null)
    {
        GameObject buttonObject = CreateImage(label + "Button", keypadPanel.transform, size, colorOverride ?? ButtonColor);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        button.transition = Selectable.Transition.ColorTint;
        button.onClick.AddListener(onClick);

        TextMeshProUGUI labelText = CreateText(label + "Label", buttonObject.transform, label, Vector2.zero, size, label.Length > 6 ? 14f : 22f, TextColor, TextAlignmentOptions.Center, true);
        labelText.raycastTarget = false;
    }

    private void RefreshInputDisplay()
    {
        if (inputDisplay == null)
        {
            return;
        }

        string visibleDigits = enteredCode.ToString().PadRight(MaximumCodeLength, '_');
        inputDisplay.text = visibleDigits;
    }

    private void SetStatus(string message, Color color)
    {
        if (statusDisplay == null)
        {
            return;
        }

        statusDisplay.text = message;
        statusDisplay.color = color;
    }
}
