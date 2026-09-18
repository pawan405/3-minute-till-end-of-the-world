using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TerminalUI : MonoBehaviour
{
    [Header("Terminal Text")]
    [SerializeField] private TMP_Text historyText;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_Text cursorText;
    [SerializeField] private TMP_InputField commandInput;
    [SerializeField] private ScrollRect historyScrollRect;

    [Header("CRT Presentation")]
    [SerializeField] private CanvasGroup flickerOverlay;
    [SerializeField, Min(0.01f)] private float cursorBlinkInterval = 0.5f;
    [SerializeField, Range(0f, 0.2f)] private float flickerStrength = 0.035f;
    [SerializeField, Min(0.001f)] private float characterInterval = 0.018f;
    [SerializeField, Min(1)] private int typingSoundEveryCharacters = 2;

    [Header("Audio")]
    [SerializeField] private AudioSource typingAudioSource;
    [SerializeField] private AudioClip typingClip;
    [SerializeField] private AudioSource inputAudioSource;
    [SerializeField] private AudioClip inputTypingClip;

    private Coroutine cursorCoroutine;
    private Coroutine flickerCoroutine;
    private bool inputEnabled;

    public TMP_InputField CommandInput => commandInput;
    public bool IsInputEnabled => inputEnabled;

    private void Awake()
    {
        if (promptText != null)
        {
            promptText.text = string.Empty;
        }

        if (cursorText != null)
        {
            cursorText.text = "_";
        }
    }

    private void OnEnable()
    {
        if (commandInput != null)
        {
            commandInput.onValueChanged.AddListener(OnCommandTextChanged);
        }

        SetCommandInputEnabled(false);
        cursorCoroutine = StartCoroutine(BlinkCursorRoutine());

        if (flickerOverlay != null)
        {
            flickerCoroutine = StartCoroutine(FlickerRoutine());
        }
    }

    private void OnDisable()
    {
        if (commandInput != null)
        {
            commandInput.onValueChanged.RemoveListener(OnCommandTextChanged);
        }

        if (cursorCoroutine != null)
        {
            StopCoroutine(cursorCoroutine);
            cursorCoroutine = null;
        }

        if (flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
            flickerCoroutine = null;
        }
    }

    /// <summary>
    /// Clears the visible terminal history and resets the active command line.
    /// </summary>
    public void ResetTerminal()
    {
        if (historyText != null)
        {
            historyText.text = string.Empty;
        }

        if (promptText != null)
        {
            promptText.text = string.Empty;
        }

        if (commandInput != null)
        {
            commandInput.text = string.Empty;
        }

        SetCommandInputEnabled(false);
        ScrollToBottom();
    }

    /// <summary>
    /// Shows or hides the prompt prefix and blinking cursor.
    /// </summary>
    public void SetPromptVisible(bool visible)
    {
        if (promptText != null)
        {
            promptText.gameObject.SetActive(visible);
        }

        if (cursorText != null)
        {
            cursorText.gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// Enables or disables command entry while preserving the terminal history.
    /// </summary>
    public void SetCommandInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        if (commandInput != null)
        {
            commandInput.interactable = enabled;
            commandInput.gameObject.SetActive(enabled);

            if (!enabled)
            {
                commandInput.text = string.Empty;
            }
        }

        SetPromptVisible(enabled);
    }

    /// <summary>
    /// Gives keyboard focus to the command field.
    /// </summary>
    public void FocusCommandInput()
    {
        if (!inputEnabled || commandInput == null)
        {
            return;
        }

        commandInput.Select();
        commandInput.ActivateInputField();
    }

    /// <summary>
    /// Sets the prompt text displayed beside the command field.
    /// </summary>
    public void SetPrompt(string prompt)
    {
        if (promptText != null)
        {
            promptText.text = prompt;
        }
    }

    /// <summary>
    /// Prints text immediately without a typewriter delay.
    /// </summary>
    public void AppendInstant(string text)
    {
        if (historyText == null || string.IsNullOrEmpty(text))
        {
            return;
        }

        historyText.text += text;
        ScrollToBottom();
    }

    /// <summary>
    /// Prints a block one character at a time with optional typing audio.
    /// </summary>
    public IEnumerator TypeText(string text, bool addTrailingLineBreak = true)
    {
        if (historyText == null || string.IsNullOrEmpty(text))
        {
            yield break;
        }

        int soundCharacterCounter = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char character = text[i];
            historyText.text += character;

            if (!char.IsWhiteSpace(character) && typingAudioSource != null && typingClip != null)
            {
                soundCharacterCounter++;
                if (soundCharacterCounter >= typingSoundEveryCharacters)
                {
                    typingAudioSource.PlayOneShot(typingClip);
                    soundCharacterCounter = 0;
                }
            }

            ScrollToBottom();
            yield return new WaitForSecondsRealtime(characterInterval);
        }

        if (addTrailingLineBreak)
        {
            historyText.text += "\n";
            ScrollToBottom();
        }
    }

    /// <summary>
    /// Adds a submitted command line using the fixed NEXUS prompt prefix.
    /// </summary>
    public void AppendCommandLine(string command)
    {
        if (historyText == null)
        {
            return;
        }

        historyText.text += $"nexus@station:~$ {command}\n";
        ScrollToBottom();
    }

    /// <summary>
    /// Clears all previously printed command history.
    /// </summary>
    public void ClearHistory()
    {
        if (historyText != null)
        {
            historyText.text = string.Empty;
        }

        ScrollToBottom();
    }

    private void OnCommandTextChanged(string value)
    {
        if (!inputEnabled || string.IsNullOrEmpty(value) || inputAudioSource == null || inputTypingClip == null)
        {
            return;
        }

        inputAudioSource.PlayOneShot(inputTypingClip);
    }

    private IEnumerator BlinkCursorRoutine()
    {
        WaitForSecondsRealtime wait = new WaitForSecondsRealtime(cursorBlinkInterval);
        while (true)
        {
            if (cursorText != null)
            {
                cursorText.enabled = !cursorText.enabled;
            }

            yield return wait;
        }
    }

    private IEnumerator FlickerRoutine()
    {
        WaitForSecondsRealtime wait = new WaitForSecondsRealtime(0.08f);
        while (true)
        {
            flickerOverlay.alpha = Random.Range(0f, flickerStrength);
            yield return wait;
        }
    }

    private void ScrollToBottom()
    {
        if (historyScrollRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();

        if (historyScrollRect.content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(historyScrollRect.content);
        }

        Canvas.ForceUpdateCanvases();
        historyScrollRect.verticalNormalizedPosition = 0f;
    }
}
