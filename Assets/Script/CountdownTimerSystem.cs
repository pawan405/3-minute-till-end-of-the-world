using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Runs the cross-scene emergency countdown and presents the shared win or timeout ending.
/// </summary>
[DisallowMultipleComponent]
public sealed class CountdownTimerSystem : MonoBehaviour
{
    private const float CountdownDurationSeconds = 180f;
    private const float CanvasReferenceWidth = 1920f;
    private const float CanvasReferenceHeight = 1080f;
    private const float HudPanelWidth = 390f;
    private const float HudPanelHeight = 100f;
    private const float OutcomePanelWidth = 1040f;
    private const float OutcomePanelHeight = 250f;
    private const float OutcomeMessageFontSize = 76f;
    private const int HudCanvasSortingOrder = 80;
    private const int OutcomeCanvasSortingOrder = 2000;
    private const string DefaultGameplaySceneName = "outpost with snow";
    private const string DefaultMainMenuSceneName = "dasboard";
    private const string VictoryMessage = "Wohooo! you win";
    private const string FailureMessage = "GAME OVER";
    private static readonly Color HudPanelColor = new Color(0.006f, 0.025f, 0.045f, 0.92f);
    private static readonly Color HudAccentColor = new Color(0.15f, 0.82f, 1f, 1f);
    private static readonly Color VictoryColor = new Color(0.34f, 0.94f, 1f, 1f);
    private static readonly Color FailureColor = new Color(1f, 0.12f, 0.1f, 1f);
    private static readonly Color OutcomePanelColor = new Color(0.008f, 0.025f, 0.055f, 0.92f);
    private static readonly Color FailurePanelColor = new Color(0.055f, 0.012f, 0.018f, 0.96f);
    private static readonly Color FailureBackdropColor = new Color(0.002f, 0.003f, 0.009f, 0.94f);
    private static readonly Color VictoryBackdropColor = new Color(0.002f, 0.012f, 0.025f, 0.86f);

    private static CountdownTimerSystem instance;
    private static bool creatingRuntimeInstance;

    [Header("Scene Routing")]
    [SerializeField] private string gameplaySceneName = DefaultGameplaySceneName;
    [SerializeField] private string mainMenuSceneName = DefaultMainMenuSceneName;

    private readonly List<Behaviour> disabledGameplayBehaviours = new List<Behaviour>();
    private float remainingSeconds = CountdownDurationSeconds;
    private TextMeshProUGUI timerText;
    private Image timerPanelImage;
    private GameObject timerHudRoot;
    private GameObject outcomeRoot;
    private bool initialized;
    private bool outcomeShown;
    private bool finalAccessCodeAccepted;

    /// <summary>Gets the persistent timer instance when the gameplay countdown has started.</summary>
    public static CountdownTimerSystem Instance => instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadStartup()
    {
        SceneManager.sceneLoaded -= OnStartupSceneLoaded;
        SceneManager.sceneLoaded += OnStartupSceneLoaded;
    }

    private static void OnStartupSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == DefaultGameplaySceneName)
        {
            EnsureRunning();
        }
    }

    private void Awake()
    {
        if (creatingRuntimeInstance)
        {
            instance = this;
            return;
        }

        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        CreatePersistentInstance();
        Destroy(this);
    }

    private void Update()
    {
        if (!initialized || outcomeShown)
        {
            return;
        }

        remainingSeconds = Mathf.Max(0f, remainingSeconds - Time.unscaledDeltaTime);
        RefreshTimerDisplay();
        if (remainingSeconds <= 0f)
        {
            TriggerGameOver();
        }
    }

    private void OnDestroy()
    {
        if (instance != this)
        {
            return;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }

    /// <summary>Records that all puzzle fragments and the final access code were accepted.</summary>
    public void MarkFinalAccessCodeAccepted()
    {
        finalAccessCodeAccepted = true;
    }


    /// <summary>Ensures a countdown exists when the space encounter is opened directly.</summary>
    public static void EnsureRunning()
    {
        if (instance == null)
        {
            CreatePersistentInstance();
        }
    }

    /// <summary>Stops the countdown and presents the shared victory animation after encounter success.</summary>
    public void CompleteGame()
    {
        if (outcomeShown || !finalAccessCodeAccepted)
        {
            return;
        }

        outcomeShown = true;
        ShowOutcome(VictoryMessage, VictoryColor, VictoryBackdropColor, false);
    }

    /// <summary>Reloads the existing gameplay level and starts a fresh three-minute attempt.</summary>
    public void RestartGameplay()
    {
        RestoreGameplayBehaviours();
        outcomeShown = false;
        finalAccessCodeAccepted = false;
        remainingSeconds = CountdownDurationSeconds;
        Time.timeScale = 1f;
        if (outcomeRoot != null)
        {
            Destroy(outcomeRoot);
            outcomeRoot = null;
        }

        if (timerHudRoot != null)
        {
            timerHudRoot.SetActive(true);
        }

        RefreshTimerDisplay();
        SceneManager.LoadScene(gameplaySceneName);
    }

    /// <summary>Returns to the project's existing main-menu scene.</summary>
    public void ReturnToMainMenu()
    {
        RestoreGameplayBehaviours();
        Time.timeScale = 1f;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
        Destroy(gameObject);
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private static void CreatePersistentInstance()
    {
        if (instance != null || creatingRuntimeInstance)
        {
            return;
        }

        creatingRuntimeInstance = true;
        GameObject runtimeRoot = new GameObject("Persistent Countdown Timer");
        DontDestroyOnLoad(runtimeRoot);
        CountdownTimerSystem timer = runtimeRoot.AddComponent<CountdownTimerSystem>();
        creatingRuntimeInstance = false;
        instance = timer;
        timer.Initialize();
    }

    private void Initialize()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        remainingSeconds = CountdownDurationSeconds;
        BuildTimerHud();
        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshTimerDisplay();
    }

    private void TriggerGameOver()
    {
        if (outcomeShown)
        {
            return;
        }

        outcomeShown = true;
        if (timerHudRoot != null)
        {
            timerHudRoot.SetActive(false);
        }

        ShowOutcome(FailureMessage, FailureColor, FailureBackdropColor, true);
    }

    private void ShowOutcome(string message, Color messageColor, Color backdropColor, bool showFailureButtons)
    {
        if (timerHudRoot != null)
        {
            timerHudRoot.SetActive(false);
        }

        DisableGameplayBehaviours();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        CanvasGroup canvasGroup;
        Image backdropImage;
        RectTransform panelRect;
        TextMeshProUGUI messageText;
        CreateOutcomeUI(message, messageColor, backdropColor, showFailureButtons,
            out canvasGroup, out backdropImage, out panelRect, out messageText);

        StartCoroutine(AnimateOutcome(canvasGroup, backdropImage, panelRect, messageText, backdropColor, messageColor, showFailureButtons));
        Time.timeScale = 0f;
    }

    private void BuildTimerHud()
    {
        timerHudRoot = new GameObject("Emergency Countdown HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        timerHudRoot.transform.SetParent(transform, false);
        Canvas canvas = timerHudRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = HudCanvasSortingOrder;
        ConfigureCanvasScaler(timerHudRoot.GetComponent<CanvasScaler>());

        RectTransform panelRect;
        timerPanelImage = CreateImage("CountdownPanel", timerHudRoot.transform, HudPanelColor, new Vector2(HudPanelWidth, HudPanelHeight),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), out panelRect);
        Outline panelOutline = timerPanelImage.gameObject.AddComponent<Outline>();
        panelOutline.effectColor = new Color(HudAccentColor.r, HudAccentColor.g, HudAccentColor.b, 0.56f);
        panelOutline.effectDistance = new Vector2(1f, -1f);
        timerPanelImage.raycastTarget = false;

        CreateText("CountdownLabel", panelRect, "NEXUS-01  //  TIME REMAINING", 14f, HudAccentColor,
            new Vector2(0f, -23f), new Vector2(HudPanelWidth - 28f, 28f), TextAlignmentOptions.Center, true);
        timerText = CreateText("CountdownValue", panelRect, "03:00", 48f, Color.white,
            new Vector2(0f, -63f), new Vector2(HudPanelWidth - 28f, 58f), TextAlignmentOptions.Center, true);
    }

    private void CreateOutcomeUI(string message, Color messageColor, Color backdropColor, bool showFailureButtons,
        out CanvasGroup canvasGroup, out Image backdropImage, out RectTransform panelRect, out TextMeshProUGUI messageText)
    {
        outcomeRoot = new GameObject("Countdown Outcome Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        Canvas canvas = outcomeRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = OutcomeCanvasSortingOrder;
        ConfigureCanvasScaler(outcomeRoot.GetComponent<CanvasScaler>());
        canvasGroup = outcomeRoot.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        backdropImage = CreateImage("OutcomeDarkenedBackground", outcomeRoot.transform, backdropColor,
            Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, out RectTransform backdropRect);
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;
        backdropImage.raycastTarget = true;

        Image panelImage = CreateImage("OutcomeMessagePanel", outcomeRoot.transform,
            showFailureButtons ? FailurePanelColor : OutcomePanelColor, new Vector2(OutcomePanelWidth, OutcomePanelHeight),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, out panelRect);
        panelImage.raycastTarget = false;
        Image topRule = CreateImage("OutcomeAccentRule", panelRect, messageColor,
            new Vector2(OutcomePanelWidth - 64f, 3f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -1f), out _);
        topRule.raycastTarget = false;

        messageText = CreateText("OutcomeMessage", panelRect, message, OutcomeMessageFontSize, messageColor,
            Vector2.zero, new Vector2(OutcomePanelWidth - 56f, OutcomePanelHeight - 40f), TextAlignmentOptions.Center, true);
        messageText.rectTransform.anchorMin = Vector2.zero;
        messageText.rectTransform.anchorMax = Vector2.one;
        messageText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        messageText.rectTransform.offsetMin = new Vector2(28f, 20f);
        messageText.rectTransform.offsetMax = new Vector2(-28f, -20f);
        messageText.rectTransform.localScale = Vector3.one * 0.58f;

        if (showFailureButtons)
        {
            CreateOutcomeButton("Restart", new Vector2(-130f, -205f), messageColor, RestartGameplay);
            CreateOutcomeButton("Main Menu", new Vector2(130f, -205f), messageColor, ReturnToMainMenu);
        }
    }

    private IEnumerator AnimateOutcome(CanvasGroup canvasGroup, Image backdropImage, RectTransform panelRect,
        TextMeshProUGUI messageText, Color backdropBaseColor, Color messageColor, bool isFailure)
    {
        const float entranceDuration = 0.72f;
        float elapsed = 0f;
        while (elapsed < entranceDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / entranceDuration);
            canvasGroup.alpha = progress;
            float scaleProgress = Mathf.Clamp01(progress / 0.72f);
            float scale = scaleProgress < 1f
                ? Mathf.Lerp(0.58f, 1.06f, scaleProgress)
                : Mathf.Lerp(1.06f, 1f, (progress - 0.72f) / 0.28f);
            panelRect.localScale = Vector3.one * scale;
            Color textColor = messageColor;
            textColor.a = progress;
            messageText.color = textColor;
            Color backdropColor = backdropBaseColor;
            backdropColor.a *= progress;
            backdropImage.color = backdropColor;
            yield return null;
        }

        panelRect.localScale = Vector3.one;
        canvasGroup.alpha = 1f;
        Color finalTextColor = messageColor;
        finalTextColor.a = 1f;
        messageText.color = finalTextColor;

        while (outcomeShown)
        {
            Color pulsingBackdrop = backdropBaseColor;
            pulsingBackdrop.a *= Mathf.Lerp(0.88f, 1f, Mathf.PingPong(Time.unscaledTime * 1.15f, 1f));
            backdropImage.color = pulsingBackdrop;
            if (isFailure)
            {
                float alarmPulse = 0.88f + Mathf.PingPong(Time.unscaledTime * 2.2f, 0.12f);
                panelRect.localPosition = new Vector3(Mathf.Sin(Time.unscaledTime * 38f) * 1.5f, 0f, 0f);
                messageText.color = new Color(messageColor.r, messageColor.g * alarmPulse, messageColor.b * alarmPulse, 1f);
            }

            yield return null;
        }
    }

    private void CreateOutcomeButton(string label, Vector2 position, Color accent, UnityEngine.Events.UnityAction onClick)
    {
        Image buttonImage = CreateImage(label + "Button", outcomeRoot.transform,
            new Color(0.035f, 0.018f, 0.024f, 0.98f), new Vector2(220f, 62f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, out _);
        buttonImage.raycastTarget = true;
        Outline outline = buttonImage.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(accent.r, accent.g, accent.b, 0.86f);
        outline.effectDistance = new Vector2(2f, -2f);
        Button button = buttonImage.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(onClick);
        TextMeshProUGUI buttonText = CreateText(label + "Label", buttonImage.transform, label,
            22f, Color.white, Vector2.zero, new Vector2(208f, 56f), TextAlignmentOptions.Center, true);
        buttonText.raycastTarget = false;
    }

    private void DisableGameplayBehaviours()
    {
        disabledGameplayBehaviours.Clear();
        Behaviour[] sceneBehaviours = FindObjectsByType<Behaviour>(FindObjectsSortMode.None);
        for (int i = 0; i < sceneBehaviours.Length; i++)
        {
            Behaviour behaviour = sceneBehaviours[i];
            if (behaviour == null || behaviour == this || !behaviour.enabled
                || behaviour.GetType().Assembly != typeof(CountdownTimerSystem).Assembly)
            {
                continue;
            }

            disabledGameplayBehaviours.Add(behaviour);
            behaviour.enabled = false;
        }
    }

    private void RestoreGameplayBehaviours()
    {
        for (int i = 0; i < disabledGameplayBehaviours.Count; i++)
        {
            if (disabledGameplayBehaviours[i] != null)
            {
                disabledGameplayBehaviours[i].enabled = true;
            }
        }

        disabledGameplayBehaviours.Clear();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (outcomeShown)
        {
            DisableGameplayBehaviours();
        }
    }

    private void RefreshTimerDisplay()
    {
        if (timerText == null)
        {
            return;
        }

        int totalSeconds = Mathf.CeilToInt(remainingSeconds);
        timerText.text = string.Format("{0:00}:{1:00}", totalSeconds / 60, totalSeconds % 60);
        if (remainingSeconds <= 20f)
        {
            float pulse = 0.72f + Mathf.PingPong(Time.unscaledTime * 2.8f, 0.28f);
            timerText.color = new Color(1f, 0.16f * pulse, 0.1f * pulse, 1f);
            timerPanelImage.color = new Color(0.12f, 0.012f, 0.018f, 0.96f);
        }
        else
        {
            timerText.color = Color.white;
            timerPanelImage.color = HudPanelColor;
        }
    }

    private static void ConfigureCanvasScaler(CanvasScaler scaler)
    {
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(CanvasReferenceWidth, CanvasReferenceHeight);
        scaler.matchWidthOrHeight = 0.5f;
    }

    private static Image CreateImage(string objectName, Transform parent, Color color, Vector2 size,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, out RectTransform rect)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static TextMeshProUGUI CreateText(string objectName, Transform parent, string textValue, float fontSize,
        Color color, Vector2 position, Vector2 size, TextAlignmentOptions alignment, bool bold)
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
        text.text = textValue;
        text.fontSize = fontSize;
        text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        text.color = color;
        text.alignment = alignment;
        text.enableAutoSizing = true;
        text.fontSizeMin = 18f;
        text.fontSizeMax = fontSize;
        text.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }

        return text;
    }
}
