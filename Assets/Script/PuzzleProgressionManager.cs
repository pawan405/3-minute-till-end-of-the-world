using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tracks puzzle completion, awards configurable access-code fragments, and displays progression.
/// </summary>
public sealed class PuzzleProgressionManager : MonoBehaviour
{
    private const int CodeFragmentLength = 2;
    private const float CanvasReferenceWidth = 1920f;
    private const float CanvasReferenceHeight = 1080f;
    private const float CanvasScaleMatch = 0.5f;
    private const int CanvasSortingOrder = 120;
    private const float AccessPanelWidth = 370f;
    private const float AccessPanelHeight = 64f;
    private const float AccessPanelLeftMargin = 22f;
    private const float AccessPanelTopMargin = 22f;
    private const float AccessPanelTextHorizontalInset = 18f;
    private const float AccessPanelTextVerticalInset = 8f;
    private const float AccessTextMinimumFontSize = 18f;
    private const float AccessTextMaximumFontSize = 25f;
    private const string DefaultPuzzle1CodeFragment = "37";
    private const string DefaultPuzzle2CodeFragment = "68";
    private const string DefaultPuzzle3CodeFragment = "04";
    private const string AccessCodeLabel = "Access Code: ";
    private const string UncollectedFragment = "__";
    private static readonly Color AccessPanelColor = new Color(0.012f, 0.035f, 0.065f, 0.82f);
    private static readonly Color AccessTextColor = new Color(0.38f, 0.85f, 1f, 1f);

    [Header("Puzzle Code Fragments")]
    [SerializeField] private string puzzle1CodeFragment = DefaultPuzzle1CodeFragment;
    [SerializeField] private string puzzle2CodeFragment = DefaultPuzzle2CodeFragment;
    [SerializeField] private string puzzle3CodeFragment = DefaultPuzzle3CodeFragment;

    [Header("Access Code UI")]
    [SerializeField] private TMP_Text accessCodeText;

    private bool puzzle1Completed;
    private bool puzzle2Completed;
    private bool puzzle3Completed;
    private GameObject generatedCanvasRoot;

    /// <summary>Gets whether all three puzzles have been completed.</summary>
    public bool AllPuzzlesCompleted => AreCodeFragmentsUnique() && puzzle1Completed && puzzle2Completed && puzzle3Completed;

    /// <summary>Gets the assembled six-digit code only after every puzzle is complete.</summary>
    public string FinalAccessCode => AllPuzzlesCompleted
        ? puzzle1CodeFragment + puzzle2CodeFragment + puzzle3CodeFragment
        : string.Empty;

    private void Awake()
    {
        ValidateCodeFragments();
        if (accessCodeText == null)
        {
            CreateAccessCodeUI();
        }

        RefreshAccessCodeDisplay();
    }

    private void OnValidate()
    {
        ValidateCodeFragments();
    }

    private void OnDestroy()
    {
        if (generatedCanvasRoot != null)
        {
            Destroy(generatedCanvasRoot);
        }
    }

    /// <summary>Reports successful completion of puzzle one; repeated calls do not award it twice.</summary>
    public void ReportPuzzle1Completed()
    {
        AwardFragment(ref puzzle1Completed, puzzle1CodeFragment, "Puzzle 1");
    }

    /// <summary>Reports successful completion of puzzle two; repeated calls do not award it twice.</summary>
    public void ReportPuzzle2Completed()
    {
        AwardFragment(ref puzzle2Completed, puzzle2CodeFragment, "Puzzle 2");
    }

    /// <summary>Reports successful completion of puzzle three; repeated calls do not award it twice.</summary>
    public void ReportPuzzle3Completed()
    {
        AwardFragment(ref puzzle3Completed, puzzle3CodeFragment, "Puzzle 3");
    }

    /// <summary>Checks an attempted code without allowing access before all puzzles are complete.</summary>
    public bool IsFinalAccessCodeCorrect(string enteredCode)
    {
        return AllPuzzlesCompleted && string.Equals(enteredCode, FinalAccessCode, StringComparison.Ordinal);
    }

    private void AwardFragment(ref bool completedFlag, string fragment, string puzzleLabel)
    {
        if (completedFlag)
        {
            return;
        }

        if (!AreCodeFragmentsUnique())
        {
            Debug.LogError("Puzzle code fragments must be valid and unique before any fragment can be awarded.", this);
            return;
        }

        if (!IsValidCodeFragment(fragment))
        {
            Debug.LogError(puzzleLabel + " code fragment must be exactly two ASCII digits from 00 to 99.", this);
            return;
        }

        completedFlag = true;
        RefreshAccessCodeDisplay();
        Debug.Log(puzzleLabel + " completed. Access-code fragment collected.", this);
    }

    private void RefreshAccessCodeDisplay()
    {
        if (accessCodeText == null)
        {
            return;
        }

        string firstFragment = puzzle1Completed ? puzzle1CodeFragment : UncollectedFragment;
        string secondFragment = puzzle2Completed ? puzzle2CodeFragment : UncollectedFragment;
        string thirdFragment = puzzle3Completed ? puzzle3CodeFragment : UncollectedFragment;
        accessCodeText.text = AccessCodeLabel + firstFragment + " " + secondFragment + " " + thirdFragment;
    }

    private void ValidateCodeFragments()
    {
        ValidateCodeFragment(puzzle1CodeFragment, "Puzzle 1");
        ValidateCodeFragment(puzzle2CodeFragment, "Puzzle 2");
        ValidateCodeFragment(puzzle3CodeFragment, "Puzzle 3");
        if (IsValidCodeFragment(puzzle1CodeFragment)
            && IsValidCodeFragment(puzzle2CodeFragment)
            && IsValidCodeFragment(puzzle3CodeFragment)
            && !AreCodeFragmentsUnique())
        {
            Debug.LogWarning("Puzzle code fragments must be unique; assign a different two-digit code to each puzzle.", this);
        }
    }

    private bool AreCodeFragmentsUnique()
    {
        return IsValidCodeFragment(puzzle1CodeFragment)
            && IsValidCodeFragment(puzzle2CodeFragment)
            && IsValidCodeFragment(puzzle3CodeFragment)
            && !string.Equals(puzzle1CodeFragment, puzzle2CodeFragment, StringComparison.Ordinal)
            && !string.Equals(puzzle1CodeFragment, puzzle3CodeFragment, StringComparison.Ordinal)
            && !string.Equals(puzzle2CodeFragment, puzzle3CodeFragment, StringComparison.Ordinal);
    }

    private void ValidateCodeFragment(string fragment, string puzzleLabel)
    {
        if (!IsValidCodeFragment(fragment))
        {
            Debug.LogWarning(puzzleLabel + " code fragment must be exactly two ASCII digits from 00 to 99.", this);
        }
    }

    private static bool IsValidCodeFragment(string fragment)
    {
        return fragment != null
            && fragment.Length == CodeFragmentLength
            && fragment[0] >= '0' && fragment[0] <= '9'
            && fragment[1] >= '0' && fragment[1] <= '9';
    }

    private void CreateAccessCodeUI()
    {
        generatedCanvasRoot = new GameObject(
            "AccessCodeCanvas",
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

        GameObject panelObject = new GameObject("AccessCodePanel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(generatedCanvasRoot.transform, false);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(AccessPanelLeftMargin, -AccessPanelTopMargin);
        panelRect.sizeDelta = new Vector2(AccessPanelWidth, AccessPanelHeight);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = AccessPanelColor;
        panelImage.raycastTarget = false;

        GameObject textObject = new GameObject("AccessCodeText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panelObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(AccessPanelTextHorizontalInset, AccessPanelTextVerticalInset);
        textRect.offsetMax = new Vector2(-AccessPanelTextHorizontalInset, -AccessPanelTextVerticalInset);

        accessCodeText = textObject.GetComponent<TextMeshProUGUI>();
        accessCodeText.text = AccessCodeLabel + UncollectedFragment + " " + UncollectedFragment + " " + UncollectedFragment;
        accessCodeText.color = AccessTextColor;
        accessCodeText.fontSize = AccessTextMaximumFontSize;
        accessCodeText.fontStyle = FontStyles.Bold;
        accessCodeText.alignment = TextAlignmentOptions.MidlineLeft;
        accessCodeText.enableAutoSizing = true;
        accessCodeText.fontSizeMin = AccessTextMinimumFontSize;
        accessCodeText.fontSizeMax = AccessTextMaximumFontSize;
        if (TMP_Settings.defaultFontAsset != null)
        {
            accessCodeText.font = TMP_Settings.defaultFontAsset;
        }
    }
}
