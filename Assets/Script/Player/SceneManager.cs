using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSceneManager : MonoBehaviour
{
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;
    private const float FadeInDuration = 0.85f;
    private const string GameplaySceneName = "outpost with snow";

    private static readonly Color BackgroundColor = new Color(0.012f, 0.027f, 0.055f, 1f);
    private static readonly Color PanelColor = new Color(0.025f, 0.055f, 0.083f, 0.96f);
    private static readonly Color CyanColor = new Color(0.16f, 0.88f, 0.96f, 1f);
    private static readonly Color MutedCyanColor = new Color(0.32f, 0.62f, 0.69f, 1f);
    private static readonly Color TextColor = new Color(0.87f, 0.95f, 0.97f, 1f);
    private static readonly Color MutedTextColor = new Color(0.48f, 0.64f, 0.69f, 1f);
    private static readonly Color WarningColor = new Color(1f, 0.25f, 0.22f, 1f);

    private CanvasGroup menuCanvasGroup;
    private GameObject howToPlayOverlay;
    private TextMeshProUGUI warningText;
    private RectTransform scanLineRect;
    private TMP_FontAsset menuFont;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Start()
    {
        Canvas existingCanvas = FindFirstObjectByType<Canvas>();
        menuFont = TMP_Settings.defaultFontAsset;
        if (existingCanvas != null)
        {
            if (menuFont == null)
            {
                TextMeshProUGUI existingText = existingCanvas.GetComponentInChildren<TextMeshProUGUI>(true);
                if (existingText != null)
                {
                    menuFont = existingText.font;
                }
            }

            existingCanvas.gameObject.SetActive(false);
        }

        BuildMainMenu();
        StartCoroutine(FadeInMenu());
    }

    private void Update()
    {
        if (howToPlayOverlay != null && howToPlayOverlay.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseHowToPlay();
        }

        if (warningText != null)
        {
            Color warningTint = warningText.color;
            warningTint.a = Mathf.Lerp(0.62f, 1f, Mathf.PingPong(Time.unscaledTime * 0.75f, 1f));
            warningText.color = warningTint;
        }

        if (scanLineRect != null)
        {
            Vector2 linePosition = scanLineRect.anchoredPosition;
            linePosition.y = Mathf.Lerp(-470f, 470f, Mathf.Repeat(Time.unscaledTime * 0.055f, 1f));
            scanLineRect.anchoredPosition = linePosition;
        }
    }

    /// <summary>Loads the existing main gameplay scene.</summary>
    public void StartGame()
    {
        SceneManager.LoadScene(GameplaySceneName);
    }

    /// <summary>Opens the optional controls and mission details overlay.</summary>
    public void HowToPlay()
    {
        if (howToPlayOverlay != null)
        {
            howToPlayOverlay.SetActive(true);
        }
    }

    /// <summary>Quits the game player.</summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void BuildMainMenu()
    {
        GameObject canvasObject = new GameObject(
            "Cinematic Main Menu",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler canvasScaler = canvasObject.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;

        menuCanvasGroup = canvasObject.AddComponent<CanvasGroup>();
        menuCanvasGroup.alpha = 0f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        Image background = CreateImage("Deep Space Backdrop", canvasRect, BackgroundColor, Vector2.zero, Vector2.zero);
        StretchToParent(background.rectTransform);

        CreateBackgroundAccents(canvasRect);
        CreateHeader(canvasRect);
        CreateLeftNavigation(canvasRect);
        CreateMissionBriefing(canvasRect);
        CreateFooter(canvasRect);
        BuildHowToPlayOverlay(canvasRect);
    }

    private void CreateBackgroundAccents(RectTransform parent)
    {
        CreateImage("Top Cyan Rule", parent, new Color(CyanColor.r, CyanColor.g, CyanColor.b, 0.42f), new Vector2(0f, 348f), new Vector2(1510f, 2f));
        CreateImage("Bottom Cyan Rule", parent, new Color(CyanColor.r, CyanColor.g, CyanColor.b, 0.18f), new Vector2(0f, -475f), new Vector2(1510f, 1f));
        CreateImage("Left Vertical Frame", parent, new Color(CyanColor.r, CyanColor.g, CyanColor.b, 0.12f), new Vector2(-832f, 0f), new Vector2(1f, 900f));
        CreateImage("Right Vertical Frame", parent, new Color(CyanColor.r, CyanColor.g, CyanColor.b, 0.12f), new Vector2(832f, 0f), new Vector2(1f, 900f));

        Image scanLine = CreateImage("Faint Reactor Scan", parent, new Color(CyanColor.r, CyanColor.g, CyanColor.b, 0.045f), new Vector2(0f, -450f), new Vector2(1450f, 1f));
        scanLineRect = scanLine.rectTransform;

        CreateText("Background Sector Label", "ORBITAL FACILITY  //  SECTOR 07", 13, MutedCyanColor, parent, new Vector2(-680f, -425f), new Vector2(420f, 24f), TextAlignmentOptions.Left, false);
        CreateText("Background Signal Label", "DISTRESS SIGNAL  //  ACTIVE", 13, new Color(WarningColor.r, WarningColor.g, WarningColor.b, 0.7f), parent, new Vector2(660f, 428f), new Vector2(360f, 24f), TextAlignmentOptions.Right, true);
    }

    private void CreateHeader(RectTransform parent)
    {
        CreateText("Network Eyebrow", "ORBITAL DEFENSE NETWORK     /     EMERGENCY ACCESS", 14, MutedCyanColor, parent, new Vector2(0f, 485f), new Vector2(1450f, 26f), TextAlignmentOptions.Center, true);
        CreateText("Game Title", "3 MINUTES TILL THE END OF THE WORLD", 42, TextColor, parent, new Vector2(0f, 427f), new Vector2(1560f, 72f), TextAlignmentOptions.Center, true);
        CreateText("Emergency Subtitle", "COLLISION COURSE CONFIRMED     //     EVERY SECOND COUNTS", 15, WarningColor, parent, new Vector2(0f, 379f), new Vector2(1200f, 28f), TextAlignmentOptions.Center, true);
    }

    private void CreateLeftNavigation(RectTransform parent)
    {
        CreateText("Navigation Eyebrow", "STATION ACCESS", 13, MutedCyanColor, parent, new Vector2(-485f, 233f), new Vector2(380f, 25f), TextAlignmentOptions.Left, true);
        CreateText("Navigation Heading", "MISSION CONTROL", 25, TextColor, parent, new Vector2(-485f, 196f), new Vector2(400f, 40f), TextAlignmentOptions.Left, true);
        CreateImage("Navigation Heading Rule", parent, new Color(CyanColor.r, CyanColor.g, CyanColor.b, 0.34f), new Vector2(-485f, 166f), new Vector2(355f, 1f));

        CreateMenuButton(parent, "START GAME", "01   /   ENTER THE FACILITY", new Vector2(-485f, 89f), true, StartGame);
        CreateMenuButton(parent, "HOW TO PLAY", "02   /   CONTROLS & OBJECTIVES", new Vector2(-485f, -10f), false, HowToPlay);
        CreateMenuButton(parent, "QUIT GAME", "03   /   CLOSE APPLICATION", new Vector2(-485f, -109f), false, QuitGame);

        CreateImage("Navigation Status Dot", parent, WarningColor, new Vector2(-658f, -253f), new Vector2(8f, 8f));
        CreateText("Navigation Status", "IMPACT ALERT     /     ACTIVE", 13, MutedTextColor, parent, new Vector2(-475f, -253f), new Vector2(365f, 24f), TextAlignmentOptions.Left, true);
        CreateText("Navigation Hint", "Restore power. Reach the launch controls.", 14, MutedTextColor, parent, new Vector2(-485f, -291f), new Vector2(390f, 48f), TextAlignmentOptions.Left, false);
    }

    private void CreateMissionBriefing(RectTransform parent)
    {
        Image panel = CreateImage("Mission Briefing Panel", parent, PanelColor, new Vector2(365f, -27f), new Vector2(790f, 660f));
        AddFrame(panel.rectTransform, 790f, 660f, new Color(CyanColor.r, CyanColor.g, CyanColor.b, 0.42f), 1.5f);
        CreateImage("Panel Header Accent", panel.rectTransform, CyanColor, new Vector2(-355f, 303f), new Vector2(4f, 24f));

        CreateText("Briefing Eyebrow", "MISSION BRIEFING     /     PRIORITY ZERO", 13, MutedCyanColor, panel.rectTransform, new Vector2(-322f, 302f), new Vector2(590f, 24f), TextAlignmentOptions.Left, true);
        CreateText("Briefing Title", "THE FINAL WINDOW", 27, TextColor, panel.rectTransform, new Vector2(-285f, 260f), new Vector2(660f, 42f), TextAlignmentOptions.Left, true);

        Image impactPanel = CreateImage("Impact Warning Panel", panel.rectTransform, new Color(0.28f, 0.045f, 0.055f, 0.64f), new Vector2(0f, 187f), new Vector2(714f, 77f));
        AddFrame(impactPanel.rectTransform, 714f, 77f, new Color(WarningColor.r, WarningColor.g, WarningColor.b, 0.78f), 1f);
        CreateImage("Impact Warning Marker", impactPanel.rectTransform, WarningColor, new Vector2(-335f, 0f), new Vector2(3f, 47f));
        warningText = CreateText("Impact Countdown", "IMPACT WINDOW     /     T–03:00", 20, WarningColor, impactPanel.rectTransform, new Vector2(-10f, 0f), new Vector2(610f, 36f), TextAlignmentOptions.Center, true);

        CreateText("Mission Situation", "A catastrophic impact is moments away. The station reactor is unstable, and the launch systems are offline. Restore power, solve the station's routing puzzles, and reach the launch controls before the window closes.", 16, TextColor, panel.rectTransform, new Vector2(0f, 101f), new Vector2(700f, 108f), TextAlignmentOptions.Left, false);
        CreateImage("Briefing Divider", panel.rectTransform, new Color(CyanColor.r, CyanColor.g, CyanColor.b, 0.24f), new Vector2(0f, 33f), new Vector2(708f, 1f));
        CreateText("Objective Label", "PRIMARY OBJECTIVES", 13, MutedCyanColor, panel.rectTransform, new Vector2(-310f, 4f), new Vector2(560f, 22f), TextAlignmentOptions.Left, true);

        CreateObjectiveRow(panel.rectTransform, "01", "RESTORE REACTOR POWER", "Bring critical station systems back online.", -47f);
        CreateObjectiveRow(panel.rectTransform, "02", "SOLVE THE ROUTING PUZZLES", "Redirect energy through the station network.", -116f);
        CreateObjectiveRow(panel.rectTransform, "03", "REACH THE LAUNCH CONTROLS", "Complete the sequence before impact.", -185f);

        CreateImage("Controls Divider", panel.rectTransform, new Color(CyanColor.r, CyanColor.g, CyanColor.b, 0.24f), new Vector2(0f, -237f), new Vector2(708f, 1f));
        CreateText("Quick Controls Label", "FIELD CONTROLS", 12, MutedCyanColor, panel.rectTransform, new Vector2(-305f, -261f), new Vector2(570f, 22f), TextAlignmentOptions.Left, true);
        CreateText("Quick Controls", "W A S D   MOVE       /       E   INTERACT", 16, TextColor, panel.rectTransform, new Vector2(0f, -294f), new Vector2(690f, 30f), TextAlignmentOptions.Center, true);
    }

    private void CreateObjectiveRow(RectTransform parent, string number, string title, string detail, float yPosition)
    {
        CreateText("Objective Number " + number, number, 16, CyanColor, parent, new Vector2(-328f, yPosition), new Vector2(48f, 26f), TextAlignmentOptions.Left, true);
        CreateText("Objective Title " + number, title, 16, TextColor, parent, new Vector2(-267f, yPosition + 4f), new Vector2(610f, 25f), TextAlignmentOptions.Left, true);
        CreateText("Objective Detail " + number, detail, 13, MutedTextColor, parent, new Vector2(-267f, yPosition - 19f), new Vector2(610f, 22f), TextAlignmentOptions.Left, false);
    }

    private void CreateFooter(RectTransform parent)
    {
        CreateText("Footer Left", "SYSTEM STATUS     /     CRITICAL", 12, MutedCyanColor, parent, new Vector2(-625f, -506f), new Vector2(460f, 22f), TextAlignmentOptions.Left, true);
        CreateText("Footer Center", "3 MINUTES IS ALL YOU HAVE", 12, MutedTextColor, parent, new Vector2(0f, -506f), new Vector2(500f, 22f), TextAlignmentOptions.Center, true);
        CreateText("Footer Right", "BUILD 07.03     /     EARTH ORBIT", 12, MutedCyanColor, parent, new Vector2(625f, -506f), new Vector2(460f, 22f), TextAlignmentOptions.Right, true);
    }

    private void BuildHowToPlayOverlay(RectTransform parent)
    {
        howToPlayOverlay = new GameObject("How To Play Overlay", typeof(RectTransform));
        RectTransform overlayRect = howToPlayOverlay.GetComponent<RectTransform>();
        overlayRect.SetParent(parent, false);
        StretchToParent(overlayRect);

        Image dimmer = CreateImage("How To Play Dimmer", overlayRect, new Color(0.006f, 0.015f, 0.03f, 0.92f), Vector2.zero, Vector2.zero);
        StretchToParent(dimmer.rectTransform);

        Image detailsPanel = CreateImage("How To Play Panel", overlayRect, PanelColor, Vector2.zero, new Vector2(760f, 570f));
        AddFrame(detailsPanel.rectTransform, 760f, 570f, new Color(CyanColor.r, CyanColor.g, CyanColor.b, 0.55f), 1.5f);
        CreateText("How To Play Eyebrow", "FIELD MANUAL     /     STATION OPERATIONS", 13, MutedCyanColor, detailsPanel.rectTransform, new Vector2(0f, 238f), new Vector2(680f, 26f), TextAlignmentOptions.Center, true);
        CreateText("How To Play Title", "HOW TO PLAY", 32, TextColor, detailsPanel.rectTransform, new Vector2(0f, 194f), new Vector2(680f, 48f), TextAlignmentOptions.Center, true);
        CreateImage("How To Play Rule", detailsPanel.rectTransform, new Color(CyanColor.r, CyanColor.g, CyanColor.b, 0.3f), new Vector2(0f, 157f), new Vector2(650f, 1f));

        CreateControlRow(detailsPanel.rectTransform, "W  A  S  D", "MOVE THROUGH THE FACILITY", 99f);
        CreateControlRow(detailsPanel.rectTransform, "E", "INTERACT WITH CONSOLES & PUZZLES", 31f);
        CreateControlRow(detailsPanel.rectTransform, "OBJECTIVES", "RESTORE POWER, ROUTE ENERGY, THEN REACH THE LAUNCH CONTROLS.", -47f);
        CreateText("How To Play Note", "Follow the prompts at each station. Every restored system brings the launch sequence closer.", 14, MutedTextColor, detailsPanel.rectTransform, new Vector2(0f, -133f), new Vector2(650f, 55f), TextAlignmentOptions.Center, false);

        CreateMenuButton(detailsPanel.rectTransform, "RETURN TO MENU", "BACK TO MISSION CONTROL", new Vector2(0f, -224f), true, CloseHowToPlay);
        howToPlayOverlay.SetActive(false);
    }

    private void CreateControlRow(RectTransform parent, string keyLabel, string description, float yPosition)
    {
        CreateText("Control Key " + keyLabel, keyLabel, 16, CyanColor, parent, new Vector2(-275f, yPosition), new Vector2(175f, 30f), TextAlignmentOptions.Left, true);
        CreateText("Control Description " + keyLabel, description, 14, TextColor, parent, new Vector2(115f, yPosition), new Vector2(500f, 38f), TextAlignmentOptions.Left, false);
    }

    private void CreateMenuButton(RectTransform parent, string label, string detail, Vector2 position, bool primary, UnityEngine.Events.UnityAction action)
    {
        Color baseColor = primary
            ? new Color(0.045f, 0.19f, 0.23f, 0.96f)
            : new Color(0.025f, 0.075f, 0.105f, 0.92f);
        Color accentColor = primary ? CyanColor : new Color(CyanColor.r, CyanColor.g, CyanColor.b, 0.54f);

        Image buttonImage = CreateImage("Menu Button " + label, parent, baseColor, position, new Vector2(390f, 78f));
        AddFrame(buttonImage.rectTransform, 390f, 78f, new Color(accentColor.r, accentColor.g, accentColor.b, primary ? 0.72f : 0.34f), 1f);
        CreateImage("Button Accent " + label, buttonImage.rectTransform, accentColor, new Vector2(-187f, 0f), new Vector2(3f, 42f));
        CreateText("Button Label " + label, label, 18, primary ? TextColor : new Color(0.78f, 0.89f, 0.92f, 1f), buttonImage.rectTransform, new Vector2(-19f, 13f), new Vector2(322f, 26f), TextAlignmentOptions.Left, true);
        CreateText("Button Detail " + label, detail, 10, MutedCyanColor, buttonImage.rectTransform, new Vector2(-19f, -15f), new Vector2(322f, 19f), TextAlignmentOptions.Left, true);
        CreateText("Button Arrow " + label, ">", 20, accentColor, buttonImage.rectTransform, new Vector2(164f, 0f), new Vector2(26f, 32f), TextAlignmentOptions.Center, true);

        buttonImage.raycastTarget = true;
        Button button = buttonImage.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.transition = Selectable.Transition.ColorTint;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.colors = new ColorBlock
        {
            normalColor = Color.white,
            highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f),
            pressedColor = new Color(0.72f, 0.92f, 0.96f, 1f),
            selectedColor = Color.white,
            disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f),
            colorMultiplier = 1f,
            fadeDuration = 0.12f
        };
        button.onClick.AddListener(action);
    }

    private Image CreateImage(string objectName, Transform parent, Color color, Vector2 position, Vector2 size)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = new Vector2(0.5f, 0.5f);
        imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.anchoredPosition = position;
        imageRect.sizeDelta = size;

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private TextMeshProUGUI CreateText(string objectName, string value, float fontSize, Color color, Transform parent, Vector2 position, Vector2 size, TextAlignmentOptions alignment, bool bold)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = position;
        textRect.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = menuFont;
        text.fontSize = fontSize;
        text.fontSizeMin = Mathf.Max(9f, fontSize * 0.78f);
        text.fontSizeMax = fontSize;
        text.enableAutoSizing = true;
        text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.enableWordWrapping = true;
        return text;
    }

    private void AddFrame(Transform parent, float width, float height, Color color, float thickness)
    {
        CreateImage("Frame Top", parent, color, new Vector2(0f, height * 0.5f - thickness * 0.5f), new Vector2(width, thickness));
        CreateImage("Frame Bottom", parent, color, new Vector2(0f, -height * 0.5f + thickness * 0.5f), new Vector2(width, thickness));
        CreateImage("Frame Left", parent, color, new Vector2(-width * 0.5f + thickness * 0.5f, 0f), new Vector2(thickness, height));
        CreateImage("Frame Right", parent, color, new Vector2(width * 0.5f - thickness * 0.5f, 0f), new Vector2(thickness, height));
    }

    private void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private void CloseHowToPlay()
    {
        if (howToPlayOverlay != null)
        {
            howToPlayOverlay.SetActive(false);
        }
    }

    private IEnumerator FadeInMenu()
    {
        float elapsed = 0f;
        while (elapsed < FadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            menuCanvasGroup.alpha = Mathf.Clamp01(elapsed / FadeInDuration);
            yield return null;
        }

        menuCanvasGroup.alpha = 1f;
    }
}
