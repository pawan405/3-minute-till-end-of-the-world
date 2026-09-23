using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds and controls a difficult 6x6 electrical circuit-routing puzzle.
/// Attach this component to a UI panel or Canvas with a RectTransform.
/// </summary>
public class ElectricalCircuitPuzzle : MonoBehaviour
{
    private const int GridSize = 6;
    private const int SourceRow = 0;
    private const int SourceColumn = 0;
    private const int ReceiverRow = 5;
    private const int ReceiverColumn = 5;

    [Header("Puzzle Layout")]
    [SerializeField] private RectTransform boardRoot;
    [SerializeField] private Text statusText;
    [SerializeField] private Text moveText;
    [SerializeField] private float cellSize = 82f;
    [SerializeField] private float cellSpacing = 7f;
    [SerializeField] private float maximumBoardSize = 900f;
    [SerializeField] private float screenPadding = 80f;

    [Header("Puzzle Generation")]
    [SerializeField] private int randomSeed = 61023;
    [SerializeField] private int minimumScrambleQuarterTurns = 30;

    [Header("Circuit Colors")]
    [SerializeField] private Color movableNodeColor = new Color(0.055f, 0.09f, 0.13f, 1f);
    [SerializeField] private Color fixedNodeColor = new Color(0.16f, 0.19f, 0.23f, 1f);
    [SerializeField] private Color sourceNodeColor = new Color(0.06f, 0.27f, 0.16f, 1f);
    [SerializeField] private Color receiverNodeColor = new Color(0.32f, 0.08f, 0.08f, 1f);
    [SerializeField] private Color connectorColor = new Color(0.20f, 0.32f, 0.40f, 1f);
    [SerializeField] private Color poweredColor = new Color(0.15f, 0.95f, 0.72f, 1f);
    [SerializeField] private Color fixedBorderColor = new Color(0.65f, 0.72f, 0.78f, 1f);

    [Header("Interface Styling")]
    [SerializeField] private Color backdropColor = new Color(0.008f, 0.012f, 0.025f, 0.90f);
    [SerializeField] private Color panelColor = new Color(0.018f, 0.035f, 0.075f, 0.98f);
    [SerializeField] private Color panelEdgeColor = new Color(0.16f, 0.42f, 0.68f, 0.85f);
    [SerializeField] private Color cellFrameColor = new Color(0.16f, 0.27f, 0.40f, 0.65f);
    [SerializeField] private Color secondaryTextColor = new Color(0.57f, 0.70f, 0.82f, 1f);
    [SerializeField] private Color redAccentColor = new Color(0.95f, 0.16f, 0.25f, 1f);
    [SerializeField] private Color blueAccentColor = new Color(0.18f, 0.62f, 1f, 1f);

    private readonly ConnectorMask[,] solutionMasks = new ConnectorMask[GridSize, GridSize];
    private readonly ConnectorMask[,] currentMasks = new ConnectorMask[GridSize, GridSize];
    private readonly NodeVisual[,] nodeVisuals = new NodeVisual[GridSize, GridSize];

    private static readonly Vector2Int[] FixedNodeCoordinates =
    {
        new Vector2Int(SourceRow, SourceColumn),
        new Vector2Int(ReceiverRow, ReceiverColumn),
        new Vector2Int(0, 1),
        new Vector2Int(1, 2),
        new Vector2Int(1, 3),
        new Vector2Int(2, 4),
        new Vector2Int(3, 4),
        new Vector2Int(4, 3),
        new Vector2Int(5, 3),
        new Vector2Int(4, 1),
        new Vector2Int(3, 0)
    };

    private static readonly ConnectorMask[] DirectionMasks =
    {
        ConnectorMask.Up,
        ConnectorMask.Right,
        ConnectorMask.Down,
        ConnectorMask.Left
    };

    private static readonly Vector2Int[] DirectionOffsets =
    {
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(1, 0),
        new Vector2Int(0, -1)
    };

    private RectTransform nodeContainer;
    private RectTransform interfacePanel;
    private Text titleText;
    private Text subtitleText;
    private Text legendText;
    private Text statusPrefixText;
    private Button resetButton;
    private Sprite solidSprite;
    private Sprite hexSprite;
    private Sprite circleSprite;
    private Sprite roundedPipeSprite;
    private Sprite roundedFillSprite;
    private Sprite roundedBorderSprite;
    private int moveCount;
    private int scrambleQuarterTurns;
    private bool puzzleSolved;

    /// <summary>
    /// Gets whether the source-to-receiver circuit has been completed.
    /// </summary>
    public bool PuzzleSolved => puzzleSolved;

    /// <summary>
    /// Rescrambles the movable nodes while preserving the guaranteed solution.
    /// </summary>
    public void ResetPuzzle()
    {
        GenerateScrambledState();
        moveCount = 0;
        puzzleSolved = false;
        UpdateVisuals();
    }

    private void Awake()
    {
        ApplyReferenceBoardStyle();
        BuildSolutionLayout();
        ValidatePuzzleContract();
        CreateBoardUi();
        GenerateScrambledState();
        UpdateVisuals();
    }

    private void ApplyReferenceBoardStyle()
    {
        movableNodeColor = new Color(0.07f, 0.32f, 0.36f, 1f);
        fixedNodeColor = new Color(0.12f, 0.42f, 0.45f, 1f);
        sourceNodeColor = new Color(0.035f, 0.42f, 0.22f, 1f);
        receiverNodeColor = new Color(0.42f, 0.22f, 0.055f, 1f);
        connectorColor = new Color(0.82f, 0.90f, 0.88f, 1f);
        poweredColor = new Color(0.48f, 1f, 0.28f, 1f);
        fixedBorderColor = new Color(0.82f, 1f, 0.98f, 1f);
        backdropColor = new Color(0.005f, 0.055f, 0.075f, 0.84f);
        panelColor = new Color(0.012f, 0.15f, 0.18f, 1f);
        panelEdgeColor = new Color(0.35f, 0.92f, 0.96f, 1f);
        cellFrameColor = new Color(0.18f, 0.58f, 0.62f, 0.85f);
        secondaryTextColor = new Color(0.76f, 0.96f, 0.96f, 1f);
        redAccentColor = new Color(1f, 0.16f, 0.12f, 1f);
        blueAccentColor = new Color(0.38f, 0.92f, 1f, 1f);
    }

    private void BuildSolutionLayout()
    {
        // The authored route is mirrored vertically so the source is top-left
        // and the receiver is bottom-right. The remaining nodes form isolated
        // loops, misleading branches, and dead ends.
        SetMirroredSolution(0, 0, ConnectorMask.Right | ConnectorMask.Down);
        SetMirroredSolution(0, 1, ConnectorMask.Left | ConnectorMask.Down);
        SetMirroredSolution(0, 2, ConnectorMask.Right | ConnectorMask.Down);
        SetMirroredSolution(0, 3, ConnectorMask.Left | ConnectorMask.Right);
        SetMirroredSolution(0, 4, ConnectorMask.Left | ConnectorMask.Down);
        SetMirroredSolution(0, 5, ConnectorMask.Down);

        SetMirroredSolution(1, 0, ConnectorMask.Up | ConnectorMask.Right);
        SetMirroredSolution(1, 1, ConnectorMask.Up | ConnectorMask.Left);
        SetMirroredSolution(1, 2, ConnectorMask.Up | ConnectorMask.Right);
        SetMirroredSolution(1, 3, ConnectorMask.Down | ConnectorMask.Left);
        SetMirroredSolution(1, 4, ConnectorMask.Up | ConnectorMask.Right);
        SetMirroredSolution(1, 5, ConnectorMask.Up | ConnectorMask.Left);

        SetMirroredSolution(2, 0, ConnectorMask.Right | ConnectorMask.Down);
        SetMirroredSolution(2, 1, ConnectorMask.Left | ConnectorMask.Down);
        SetMirroredSolution(2, 2, ConnectorMask.Right | ConnectorMask.Down);
        SetMirroredSolution(2, 3, ConnectorMask.Up | ConnectorMask.Right);
        SetMirroredSolution(2, 4, ConnectorMask.Down | ConnectorMask.Left);
        SetMirroredSolution(2, 5, ConnectorMask.Left);

        SetMirroredSolution(3, 0, ConnectorMask.Up | ConnectorMask.Right);
        SetMirroredSolution(3, 1, ConnectorMask.Up | ConnectorMask.Left);
        SetMirroredSolution(3, 2, ConnectorMask.Up | ConnectorMask.Right | ConnectorMask.Down);
        SetMirroredSolution(3, 3, ConnectorMask.Left);
        SetMirroredSolution(3, 4, ConnectorMask.Up | ConnectorMask.Right);
        SetMirroredSolution(3, 5, ConnectorMask.Down | ConnectorMask.Left);

        SetMirroredSolution(4, 0, ConnectorMask.Up | ConnectorMask.Right);
        SetMirroredSolution(4, 1, ConnectorMask.Right | ConnectorMask.Down);
        SetMirroredSolution(4, 2, ConnectorMask.Left | ConnectorMask.Right);
        SetMirroredSolution(4, 3, ConnectorMask.Left | ConnectorMask.Down);
        SetMirroredSolution(4, 4, ConnectorMask.Up | ConnectorMask.Right | ConnectorMask.Down | ConnectorMask.Left);
        SetMirroredSolution(4, 5, ConnectorMask.Up | ConnectorMask.Down);

        SetMirroredSolution(5, 0, ConnectorMask.Right);
        SetMirroredSolution(5, 1, ConnectorMask.Up | ConnectorMask.Left);
        SetMirroredSolution(5, 2, ConnectorMask.Up | ConnectorMask.Right | ConnectorMask.Left);
        SetMirroredSolution(5, 3, ConnectorMask.Up | ConnectorMask.Right);
        SetMirroredSolution(5, 4, ConnectorMask.Left | ConnectorMask.Right);
        SetMirroredSolution(5, 5, ConnectorMask.Up | ConnectorMask.Left);
    }

    private void SetMirroredSolution(int authoredRow, int column, ConnectorMask authoredMask)
    {
        solutionMasks[GridSize - 1 - authoredRow, column] = FlipVertical(authoredMask);
    }

    private static ConnectorMask FlipVertical(ConnectorMask mask)
    {
        ConnectorMask flipped = ConnectorMask.None;
        if (HasConnection(mask, ConnectorMask.Up))
            flipped |= ConnectorMask.Down;
        if (HasConnection(mask, ConnectorMask.Right))
            flipped |= ConnectorMask.Right;
        if (HasConnection(mask, ConnectorMask.Down))
            flipped |= ConnectorMask.Up;
        if (HasConnection(mask, ConnectorMask.Left))
            flipped |= ConnectorMask.Left;
        return flipped;
    }

    private void ValidatePuzzleContract()
    {
        for (int row = 0; row < GridSize; row++)
        {
            for (int column = 0; column < GridSize; column++)
            {
                ConnectorMask mask = solutionMasks[row, column];
                if (mask == ConnectorMask.None)
                    throw new InvalidOperationException("The circuit layout contains an empty node.");

                if (row == 0 && HasConnection(mask, ConnectorMask.Up))
                    throw new InvalidOperationException("A circuit node points outside the top edge.");
                if (row == GridSize - 1 && HasConnection(mask, ConnectorMask.Down))
                    throw new InvalidOperationException("A circuit node points outside the bottom edge.");
                if (column == 0 && HasConnection(mask, ConnectorMask.Left))
                    throw new InvalidOperationException("A circuit node points outside the left edge.");
                if (column == GridSize - 1 && HasConnection(mask, ConnectorMask.Right))
                    throw new InvalidOperationException("A circuit node points outside the right edge.");
            }
        }

        int pathCount = CountPaths(solutionMasks, SourceRow, SourceColumn, ReceiverRow, ReceiverColumn, new bool[GridSize, GridSize]);
        if (pathCount != 1)
            throw new InvalidOperationException("The circuit solution must contain exactly one source-to-receiver path.");

        for (int i = 0; i < FixedNodeCoordinates.Length; i++)
        {
            Vector2Int fixedNode = FixedNodeCoordinates[i];
            if (solutionMasks[fixedNode.x, fixedNode.y] == ConnectorMask.None)
                throw new InvalidOperationException("A fixed node has no valid solution orientation.");
        }
    }

    private void CreateBoardUi()
    {
        solidSprite = CreateSolidSprite();
        hexSprite = CreateHexSprite();
        circleSprite = CreateCircleSprite();
        roundedFillSprite = CreateRoundedNodeSprite(false);
        roundedBorderSprite = CreateRoundedNodeSprite(true);
        roundedPipeSprite = CreateRoundedPipeSprite();

        RectTransform hostRect = transform as RectTransform;
        float availableWidth = maximumBoardSize;
        float availableHeight = maximumBoardSize;
        if (hostRect != null && hostRect.rect.width > 0f && hostRect.rect.height > 0f)
        {
            availableWidth = hostRect.rect.width - screenPadding * 2f;
            availableHeight = hostRect.rect.height - screenPadding * 2f - 190f;
        }

        float boardSize = 760f;
        cellSpacing = 8f;
        cellSize = (boardSize - (GridSize - 1) * cellSpacing) / GridSize;

        CreateInterfaceChrome(boardSize);

        if (boardRoot == null)
        {
            GameObject boardObject = new GameObject("CircuitBoard", typeof(RectTransform));
            boardObject.transform.SetParent(transform, false);
            boardRoot = boardObject.GetComponent<RectTransform>();
        }

        boardRoot.anchorMin = new Vector2(0.5f, 0.5f);
        boardRoot.anchorMax = new Vector2(0.5f, 0.5f);
        boardRoot.pivot = new Vector2(0.5f, 0.5f);
        boardRoot.anchoredPosition = new Vector2(-145f, -22f);
        boardRoot.sizeDelta = new Vector2(boardSize, boardSize);

        Image boardSurface = CreateImage("BoardSurface", transform, new Vector2(boardSize + 18f, boardSize + 18f), new Vector2(-145f, -22f));
        boardSurface.color = new Color(0.015f, 0.10f, 0.12f, 1f);
        boardSurface.raycastTarget = false;
        boardSurface.transform.SetAsFirstSibling();

        GameObject containerObject = new GameObject("CircuitNodes", typeof(RectTransform), typeof(GridLayoutGroup));
        containerObject.transform.SetParent(boardRoot, false);
        nodeContainer = containerObject.GetComponent<RectTransform>();
        nodeContainer.anchorMin = Vector2.zero;
        nodeContainer.anchorMax = Vector2.one;
        nodeContainer.offsetMin = Vector2.zero;
        nodeContainer.offsetMax = Vector2.zero;

        GridLayoutGroup grid = containerObject.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(cellSize, cellSize);
        grid.spacing = new Vector2(cellSpacing, cellSpacing);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = GridSize;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;

        CreateCoordinateLabels(boardSize, cellSize);

        for (int row = 0; row < GridSize; row++)
        {
            for (int column = 0; column < GridSize; column++)
                CreateNodeVisual(row, column);
        }
    }

    private void CreateCoordinateLabels(float boardSize, float calculatedCellSize)
    {
        float firstCenter = -boardSize * 0.5f + calculatedCellSize * 0.5f;
        float step = calculatedCellSize + cellSpacing;
        for (int column = 0; column < GridSize; column++)
        {
            Text label = CreateLabel("Column_" + column, transform, 15, TextAnchor.MiddleCenter);
            StyleHeaderLabel(label, new Vector2(-145f + firstCenter + column * step, boardSize * 0.5f + 4f), new Vector2(24f, 18f), secondaryTextColor, FontStyle.Bold);
            label.text = (column + 1).ToString();
        }

        for (int row = 0; row < GridSize; row++)
        {
            Text label = CreateLabel("Row_" + row, transform, 15, TextAnchor.MiddleCenter);
            StyleHeaderLabel(label, new Vector2(-145f - boardSize * 0.5f - 18f, -22f + boardSize * 0.5f - calculatedCellSize * 0.5f - row * step), new Vector2(24f, 18f), secondaryTextColor, FontStyle.Bold);
            label.text = (row + 1).ToString();
        }
    }


    private void CreateInterfaceChrome(float boardSize)
    {
        GameObject backdropObject = new GameObject("CircuitBackdrop", typeof(RectTransform), typeof(Image));
        backdropObject.transform.SetParent(transform, false);
        RectTransform backdropRect = backdropObject.GetComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;
        Image backdrop = backdropObject.GetComponent<Image>();
        backdrop.sprite = solidSprite;
        backdrop.color = backdropColor;
        backdrop.raycastTarget = false;
        backdropObject.transform.SetAsFirstSibling();

        GameObject panelObject = new GameObject("CircuitInterfacePanel", typeof(RectTransform), typeof(Image), typeof(Shadow));
        panelObject.transform.SetParent(transform, false);
        interfacePanel = panelObject.GetComponent<RectTransform>();
        interfacePanel.anchorMin = new Vector2(0.5f, 0.5f);
        interfacePanel.anchorMax = new Vector2(0.5f, 0.5f);
        interfacePanel.pivot = new Vector2(0.5f, 0.5f);
        interfacePanel.anchoredPosition = new Vector2(0f, -10f);
        interfacePanel.sizeDelta = new Vector2(boardSize + 300f, boardSize + 320f);
        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.sprite = solidSprite;
        panelImage.color = panelColor;
        panelImage.raycastTarget = false;
        Shadow panelShadow = panelObject.GetComponent<Shadow>();
        panelShadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
        panelShadow.effectDistance = new Vector2(0f, -10f);
        panelShadow.useGraphicAlpha = true;
        panelObject.transform.SetAsFirstSibling();

        float panelWidth = boardSize + 300f;
        float panelHeight = boardSize + 320f;
        CreateImage("TopAccent", panelObject.transform, new Vector2(panelWidth - 44f, 3f), new Vector2(0f, panelHeight * 0.5f - 19f)).color = panelEdgeColor;
        CreateImage("TopAccentRed", panelObject.transform, new Vector2(92f, 3f), new Vector2(-panelWidth * 0.5f + 62f, panelHeight * 0.5f - 19f)).color = redAccentColor;
        CreateImage("BottomAccent", panelObject.transform, new Vector2(panelWidth - 44f, 2f), new Vector2(0f, -panelHeight * 0.5f + 20f)).color = new Color(blueAccentColor.r, blueAccentColor.g, blueAccentColor.b, 0.55f);
        CreateImage("LeftAccent", panelObject.transform, new Vector2(2f, panelHeight - 44f), new Vector2(-panelWidth * 0.5f + 20f, 0f)).color = new Color(redAccentColor.r, redAccentColor.g, redAccentColor.b, 0.65f);
        Text brandText = CreateLabel("BrandText", transform, 14, TextAnchor.MiddleLeft);
        StyleHeaderLabel(brandText, new Vector2(-350f, panelHeight * 0.5f - 88f), new Vector2(130f, 24f), secondaryTextColor, FontStyle.Bold);
        brandText.text = "◈  AURORA";
        Text panelTag = CreateLabel("PanelTag", transform, 11, TextAnchor.MiddleRight);
        StyleHeaderLabel(panelTag, new Vector2(panelWidth * 0.5f - 94f, panelHeight * 0.5f - 88f), new Vector2(150f, 38f), secondaryTextColor, FontStyle.Normal);
        panelTag.text = "CLEANER\nBRIGHTER\nTOMORROW";


        CreateImage("RightAccent", panelObject.transform, new Vector2(2f, panelHeight - 44f), new Vector2(panelWidth * 0.5f - 20f, 0f)).color = new Color(blueAccentColor.r, blueAccentColor.g, blueAccentColor.b, 0.65f);

        titleText = CreateLabel("CircuitTitle", transform, 32, TextAnchor.MiddleLeft);
        StyleHeaderLabel(titleText, new Vector2(-115f, boardSize * 0.5f + 108f), new Vector2(420f, 38f), Color.white, FontStyle.Bold);
        titleText.text = "POWER ROUTING SYSTEM";

        subtitleText = CreateLabel("CircuitSubtitle", transform, 15, TextAnchor.MiddleLeft);
        StyleHeaderLabel(subtitleText, new Vector2(-115f, boardSize * 0.5f + 82f), new Vector2(420f, 24f), secondaryTextColor, FontStyle.Normal);
        subtitleText.text = "ROTATE THE NODES TO RESTORE POWER.";

        Image statusBox = CreateImage("StatusBox", transform, new Vector2(boardSize - 10f, 56f), new Vector2(-145f, boardSize * 0.5f + 34f));
        statusBox.color = new Color(0.02f, 0.14f, 0.17f, 1f);
        statusBox.raycastTarget = false;
        statusPrefixText = CreateLabel("StatusPrefix", transform, 20, TextAnchor.MiddleLeft);
        StyleHeaderLabel(statusPrefixText, new Vector2(-370f, boardSize * 0.5f + 34f), new Vector2(100f, 46f), secondaryTextColor, FontStyle.Bold);
        statusPrefixText.text = "STATUS:";
        statusText = CreateLabel("CircuitStatus", transform, 20, TextAnchor.MiddleLeft);
        StyleHeaderLabel(statusText, new Vector2(-220f, boardSize * 0.5f + 34f), new Vector2(140f, 46f), redAccentColor, FontStyle.Bold);

        Image warningBox = CreateImage("WarningBox", transform, new Vector2(300f, 56f), new Vector2(245f, boardSize * 0.5f + 34f));
        warningBox.color = new Color(0.18f, 0.07f, 0.06f, 0.98f);
        warningBox.raycastTarget = false;
        Text warningText = CreateLabel("RouteWarning", transform, 13, TextAnchor.MiddleCenter);
        StyleHeaderLabel(warningText, new Vector2(245f, boardSize * 0.5f + 34f), new Vector2(290f, 46f), new Color(1f, 0.78f, 0.58f, 1f), FontStyle.Bold);
        warningText.text = "!   ROUTE POWER FROM SOURCE TO RECEIVER";

        moveText = CreateLabel("CircuitMoves", transform, 18, TextAnchor.MiddleLeft);
        StyleHeaderLabel(moveText, new Vector2(-300f, -boardSize * 0.5f - 122f), new Vector2(180f, 42f), secondaryTextColor, FontStyle.Bold);

        Image footerBox = CreateImage("FooterBox", transform, new Vector2(boardSize + 180f, 54f), new Vector2(-45f, -boardSize * 0.5f - 122f));
        footerBox.color = new Color(0.01f, 0.06f, 0.08f, 0.98f);
        footerBox.raycastTarget = false;
        moveText.transform.SetAsLastSibling();

        legendText = CreateLabel("CircuitLegend", transform, 14, TextAnchor.UpperLeft);
        StyleHeaderLabel(legendText, new Vector2(390f, -boardSize * 0.5f + 72f), new Vector2(220f, 82f), secondaryTextColor, FontStyle.Normal);
        legendText.text = "STATUS INDICATORS\n\nLOCK        Fixed Node\nCLICK       Movable Node";

        Text legendTitle = CreateLabel("NodeTypesTitle", transform, 18, TextAnchor.MiddleLeft);
        StyleHeaderLabel(legendTitle, new Vector2(390f, boardSize * 0.5f - 38f), new Vector2(220f, 28f), Color.white, FontStyle.Bold);
        legendTitle.text = "NODE TYPES";
        CreateLegendSamples(boardSize);
        CreateResetButton(boardSize);
    }

    private void StyleHeaderLabel(Text label, Vector2 position, Vector2 size, Color color, FontStyle style)
    {
        RectTransform rect = label.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        label.color = color;
        label.fontStyle = style;
        label.raycastTarget = false;
    }

    private void CreateLegendSamples(float boardSize)
    {
        ConnectorMask[] masks =
        {
            ConnectorMask.Up | ConnectorMask.Down,
            ConnectorMask.Up | ConnectorMask.Right,
            ConnectorMask.Up | ConnectorMask.Left | ConnectorMask.Right,
            ConnectorMask.Up | ConnectorMask.Right | ConnectorMask.Down | ConnectorMask.Left,
            ConnectorMask.Down
        };
        string[] names = { "Straight", "Corner", "T-Junction", "Cross", "Dead End" };
        for (int index = 0; index < masks.Length; index++)
        {
            float y = boardSize * 0.5f - 82f - index * 42f;
            Image sample = CreateImage("LegendNode_" + index, transform, new Vector2(32f, 32f), new Vector2(325f, y));
            sample.color = new Color(0.04f, 0.18f, 0.21f, 1f);
            for (int direction = 0; direction < DirectionMasks.Length; direction++)
            {
                if (!HasConnection(masks[index], DirectionMasks[direction]))
                    continue;
                Vector2 size = direction == 0 || direction == 2 ? new Vector2(4f, 14f) : new Vector2(14f, 4f);
                Vector2 position = direction == 0 ? new Vector2(0f, 6f) : direction == 1 ? new Vector2(6f, 0f) : direction == 2 ? new Vector2(0f, -6f) : new Vector2(-6f, 0f);
                Image connector = CreateImage("LegendConnector_" + index + "_" + direction, transform, size, new Vector2(325f + position.x, y + position.y));
                connector.color = new Color(0.65f, 0.72f, 0.72f, 1f);
            }
            Text label = CreateLabel("LegendLabel_" + index, transform, 14, TextAnchor.MiddleLeft);
            StyleHeaderLabel(label, new Vector2(425f, y), new Vector2(150f, 28f), secondaryTextColor, FontStyle.Normal);
            label.text = names[index];
        }

        Image sourceIcon = CreateImage("LegendSource", transform, new Vector2(30f, 30f), new Vector2(325f, -boardSize * 0.5f + 205f));
        sourceIcon.sprite = circleSprite;
        sourceIcon.color = poweredColor;
        Image receiverIcon = CreateImage("LegendReceiver", transform, new Vector2(30f, 30f), new Vector2(325f, -boardSize * 0.5f + 160f));
        receiverIcon.sprite = circleSprite;
        receiverIcon.color = new Color(1f, 0.12f, 0.10f, 1f);
        Text sourceLabel = CreateLabel("LegendSourceLabel", transform, 12, TextAnchor.MiddleLeft);
        StyleHeaderLabel(sourceLabel, new Vector2(425f, -boardSize * 0.5f + 205f), new Vector2(150f, 28f), secondaryTextColor, FontStyle.Normal);
        sourceLabel.text = "SOURCE  (FIXED)";
        Text receiverLabel = CreateLabel("LegendReceiverLabel", transform, 12, TextAnchor.MiddleLeft);
        StyleHeaderLabel(receiverLabel, new Vector2(425f, -boardSize * 0.5f + 160f), new Vector2(150f, 28f), secondaryTextColor, FontStyle.Normal);
        receiverLabel.text = "RECEIVER  (FIXED)";
    }


    private void CreateResetButton(float boardSize)
    {
        GameObject buttonObject = new GameObject("ResetCircuitButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(transform, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(235f, -boardSize * 0.5f - 122f);
        buttonRect.sizeDelta = new Vector2(112f, 36f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.sprite = solidSprite;
        buttonImage.color = new Color(blueAccentColor.r, blueAccentColor.g, blueAccentColor.b, 0.14f);
        buttonImage.raycastTarget = true;

        resetButton = buttonObject.GetComponent<Button>();
        resetButton.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = resetButton.colors;
        colors.normalColor = new Color(blueAccentColor.r, blueAccentColor.g, blueAccentColor.b, 0.14f);
        colors.highlightedColor = new Color(blueAccentColor.r, blueAccentColor.g, blueAccentColor.b, 0.32f);
        colors.pressedColor = new Color(blueAccentColor.r, blueAccentColor.g, blueAccentColor.b, 0.48f);
        colors.selectedColor = colors.highlightedColor;
        resetButton.colors = colors;
        resetButton.onClick.AddListener(ResetPuzzle);

        Text buttonLabel = CreateLabel("ResetLabel", buttonObject.transform, 14, TextAnchor.MiddleCenter);
        buttonLabel.text = "RESET GRID";
        Text footerInstruction = CreateLabel("FooterInstruction", transform, 14, TextAnchor.MiddleLeft);
        StyleHeaderLabel(footerInstruction, new Vector2(25f, -boardSize * 0.5f - 122f), new Vector2(255f, 38f), secondaryTextColor, FontStyle.Normal);
        footerInstruction.text = "Click a node to rotate it 90 degrees.\nCreate a continuous path to restore power.";


        buttonLabel.color = blueAccentColor;
        buttonLabel.fontStyle = FontStyle.Bold;
        RectTransform labelRect = buttonLabel.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }

    private void CreateNodeVisual(int row, int column)
    {
        GameObject nodeObject = new GameObject("Node_" + row + "_" + column, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Shadow));
        nodeObject.transform.SetParent(nodeContainer, false);

        Image background = nodeObject.GetComponent<Image>();
        background.sprite = solidSprite;
        background.color = movableNodeColor;

        Shadow nodeShadow = nodeObject.GetComponent<Shadow>();
        nodeShadow.effectColor = new Color(0f, 0f, 0f, 0.42f);
        nodeShadow.effectDistance = new Vector2(0f, -3f);
        nodeShadow.useGraphicAlpha = true;

        Button button = nodeObject.GetComponent<Button>();
        int capturedRow = row;
        int capturedColumn = column;
        button.onClick.AddListener(() => RotateNode(capturedRow, capturedColumn));
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock buttonColors = button.colors;
        buttonColors.normalColor = Color.white;
        buttonColors.highlightedColor = new Color(0.72f, 0.90f, 1f, 1f);
        buttonColors.pressedColor = new Color(0.55f, 0.78f, 1f, 1f);
        button.colors = buttonColors;

        NodeVisual visual = new NodeVisual
        {
            background = background,
            center = CreateImage("Center", nodeObject.transform, new Vector2(28f, 28f), Vector2.zero),
            typeLabel = CreateLabel("NodeType", nodeObject.transform, 12, TextAnchor.MiddleCenter),
            fixedLabel = CreateLabel("FixedMarker", nodeObject.transform, 12, TextAnchor.MiddleCenter),
            border = CreateImage("FixedBorder", nodeObject.transform, new Vector2(cellSize - 4f, cellSize - 4f), Vector2.zero),
            cellFrame = CreateImage("CellFrame", nodeObject.transform, new Vector2(cellSize - 5f, cellSize - 5f), Vector2.zero)
        };

        background.sprite = roundedFillSprite;
        visual.border.sprite = roundedBorderSprite;
        visual.cellFrame.sprite = roundedBorderSprite;


        visual.edgeLines[0] = CreateImage("TopEdge", nodeObject.transform, new Vector2(cellSize - 8f, 2f), new Vector2(0f, cellSize * 0.5f - 4f));
        visual.edgeLines[1] = CreateImage("RightEdge", nodeObject.transform, new Vector2(2f, cellSize - 8f), new Vector2(cellSize * 0.5f - 4f, 0f));
        visual.edgeLines[2] = CreateImage("BottomEdge", nodeObject.transform, new Vector2(cellSize - 8f, 2f), new Vector2(0f, -cellSize * 0.5f + 4f));
        visual.edgeLines[3] = CreateImage("LeftEdge", nodeObject.transform, new Vector2(2f, cellSize - 8f), new Vector2(-cellSize * 0.5f + 4f, 0f));
        visual.cornerMarkers[0] = CreateImage("TopLeftCorner", nodeObject.transform, new Vector2(5f, 5f), new Vector2(-cellSize * 0.5f + 5f, cellSize * 0.5f - 5f));
        visual.cornerMarkers[1] = CreateImage("TopRightCorner", nodeObject.transform, new Vector2(5f, 5f), new Vector2(cellSize * 0.5f - 5f, cellSize * 0.5f - 5f));
        visual.cornerMarkers[2] = CreateImage("BottomRightCorner", nodeObject.transform, new Vector2(5f, 5f), new Vector2(cellSize * 0.5f - 5f, -cellSize * 0.5f + 5f));
        visual.cornerMarkers[3] = CreateImage("BottomLeftCorner", nodeObject.transform, new Vector2(5f, 5f), new Vector2(-cellSize * 0.5f + 5f, -cellSize * 0.5f + 5f));



        visual.connectors[0] = CreateImage("UpConnection", nodeObject.transform, new Vector2(14f, cellSize * 0.43f), new Vector2(0f, cellSize * 0.22f));
        visual.connectors[1] = CreateImage("RightConnection", nodeObject.transform, new Vector2(cellSize * 0.43f, 14f), new Vector2(cellSize * 0.22f, 0f));
        visual.connectors[2] = CreateImage("DownConnection", nodeObject.transform, new Vector2(14f, cellSize * 0.43f), new Vector2(0f, -cellSize * 0.22f));
        visual.connectors[3] = CreateImage("LeftConnection", nodeObject.transform, new Vector2(cellSize * 0.43f, 14f), new Vector2(-cellSize * 0.22f, 0f));
        visual.connectorGlows[0] = CreateImage("UpGlow", nodeObject.transform, new Vector2(26f, cellSize * 0.45f), new Vector2(0f, cellSize * 0.22f));
        visual.connectorGlows[1] = CreateImage("RightGlow", nodeObject.transform, new Vector2(cellSize * 0.45f, 26f), new Vector2(cellSize * 0.22f, 0f));
        visual.connectorGlows[2] = CreateImage("DownGlow", nodeObject.transform, new Vector2(26f, cellSize * 0.45f), new Vector2(0f, -cellSize * 0.22f));
        visual.connectorGlows[3] = CreateImage("LeftGlow", nodeObject.transform, new Vector2(cellSize * 0.45f, 26f), new Vector2(-cellSize * 0.22f, 0f));

        visual.typeLabel.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        visual.typeLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        visual.typeLabel.rectTransform.sizeDelta = new Vector2(cellSize, 22f);
        visual.typeLabel.rectTransform.anchoredPosition = new Vector2(0f, -cellSize * 0.30f);
        visual.typeLabel.color = Color.white;
        visual.typeLabel.fontStyle = FontStyle.Bold;
        for (int direction = 0; direction < DirectionMasks.Length; direction++)
        {
            visual.connectors[direction].sprite = roundedPipeSprite;
            visual.connectors[direction].type = Image.Type.Simple;
            visual.connectorGlows[direction].gameObject.SetActive(false);
        }


        visual.typeLabel.raycastTarget = false;

        visual.fixedLabel.rectTransform.anchorMin = new Vector2(1f, 1f);
        visual.fixedLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
        visual.fixedLabel.rectTransform.pivot = new Vector2(1f, 1f);
        visual.fixedLabel.rectTransform.sizeDelta = new Vector2(20f, 18f);
        visual.fixedLabel.rectTransform.anchoredPosition = new Vector2(-4f, -4f);
        visual.fixedLabel.color = fixedBorderColor;
        visual.fixedLabel.fontStyle = FontStyle.Bold;
        visual.fixedLabel.raycastTarget = false;

        visual.border.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        visual.border.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        visual.border.rectTransform.SetAsFirstSibling();
        visual.border.color = new Color(fixedBorderColor.r, fixedBorderColor.g, fixedBorderColor.b, 0.12f);
        visual.border.raycastTarget = false;

        visual.cellFrame.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        visual.cellFrame.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        visual.cellFrame.rectTransform.SetAsFirstSibling();
        visual.cellFrame.color = cellFrameColor;
        visual.cellFrame.raycastTarget = false;

        visual.center.color = connectorColor;
        visual.center.raycastTarget = false;
        for (int edge = 0; edge < visual.edgeLines.Length; edge++)
        {
            visual.edgeLines[edge].color = cellFrameColor;
            visual.edgeLines[edge].raycastTarget = false;
            visual.edgeLines[edge].transform.SetAsFirstSibling();
        }
        for (int corner = 0; corner < visual.cornerMarkers.Length; corner++)
        {
            visual.cornerMarkers[corner].color = blueAccentColor;
            visual.cornerMarkers[corner].raycastTarget = false;
        }
        for (int direction = 0; direction < visual.connectorGlows.Length; direction++)
        {
            visual.connectorGlows[direction].color = new Color(connectorColor.r, connectorColor.g, connectorColor.b, 0.12f);
            visual.connectorGlows[direction].raycastTarget = false;
            visual.connectorGlows[direction].transform.SetAsFirstSibling();
        }

        visual.nodeObject = nodeObject;
        nodeVisuals[row, column] = visual;
    }

    private Sprite CreateSolidSprite()
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = "ElectricalCircuitPuzzleSolidTexture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateCircleSprite()
    {
        const int textureSize = 64;
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        texture.name = "ElectricalCircuitPuzzleCircleTexture";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Vector2 center = new Vector2(textureSize * 0.5f, textureSize * 0.5f);
        float radius = textureSize * 0.47f;
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float alpha = Mathf.Clamp01(radius - distance + 1f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f));
    }
    private Sprite CreateHexSprite()
    {
        const int textureSize = 128;
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        texture.name = "ElectricalCircuitPuzzleHexTexture";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Vector2[] vertices = new Vector2[6];
        for (int index = 0; index < vertices.Length; index++)
        {
            float angle = Mathf.Deg2Rad * (index * 60f);
            vertices[index] = new Vector2(Mathf.Cos(angle) * 0.94f, Mathf.Sin(angle) * 0.94f);
        }

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                Vector2 point = new Vector2((x + 0.5f) / textureSize * 2f - 1f, (y + 0.5f) / textureSize * 2f - 1f);
                bool inside = true;
                float winding = 0f;
                for (int edge = 0; edge < vertices.Length; edge++)
                {
                    Vector2 start = vertices[edge];
                    Vector2 end = vertices[(edge + 1) % vertices.Length];
                    float cross = (end.x - start.x) * (point.y - start.y) - (end.y - start.y) * (point.x - start.x);
                    if (edge == 0)
                        winding = cross;
                    else if (cross * winding < 0f)
                        inside = false;
                }

                float alpha = inside ? 1f : 0f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f));
    }



    private Sprite CreateRoundedNodeSprite(bool borderOnly)
    {
        const int textureSize = 128;
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        texture.name = borderOnly ? "ElectricalCircuitPuzzleRoundedBorder" : "ElectricalCircuitPuzzleRoundedFill";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2(textureSize * 0.5f, textureSize * 0.5f);
        float outerHalfSize = 61f;
        float outerRadius = 15f;
        float innerHalfSize = 57f;
        float innerRadius = 12f;
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                Vector2 point = new Vector2(x + 0.5f, y + 0.5f) - center;
                float outerDistance = RoundedRectDistance(point, outerHalfSize, outerRadius);
                float innerDistance = RoundedRectDistance(point, innerHalfSize, innerRadius);
                bool insideOuter = outerDistance <= 0f;
                bool insideInner = innerDistance <= 0f;
                float alpha = borderOnly
                    ? Mathf.Clamp01(Mathf.Max(outerDistance + 1f, -innerDistance + 1f))
                    : insideOuter ? 1f : 0f;
                if (borderOnly && insideInner)
                    alpha = 0f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f));
    }

    private static float RoundedRectDistance(Vector2 point, float halfSize, float radius)
    {
        Vector2 distance = new Vector2(Mathf.Abs(point.x), Mathf.Abs(point.y)) - new Vector2(halfSize - radius, halfSize - radius);
        Vector2 outside = new Vector2(Mathf.Max(distance.x, 0f), Mathf.Max(distance.y, 0f));
        return outside.magnitude + Mathf.Min(Mathf.Max(distance.x, distance.y), 0f) - radius;
    }

    private Sprite CreateRoundedPipeSprite()
    {
        const int textureWidth = 128;
        const int textureHeight = 48;
        Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        texture.name = "ElectricalCircuitPuzzleRoundedPipe";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Vector2 center = new Vector2(textureWidth * 0.5f, textureHeight * 0.5f);
        float halfLength = textureWidth * 0.5f;
        float radius = textureHeight * 0.5f - 1f;
        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                Vector2 point = new Vector2(Mathf.Abs(x + 0.5f - center.x), Mathf.Abs(y + 0.5f - center.y));
                Vector2 distance = point - new Vector2(halfLength - radius, radius);
                Vector2 outside = new Vector2(Mathf.Max(distance.x, 0f), Mathf.Max(distance.y, 0f));
                float signedDistance = outside.magnitude + Mathf.Min(Mathf.Max(distance.x, distance.y), 0f) - radius;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(-signedDistance + 1f)));
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, textureWidth, textureHeight), new Vector2(0.5f, 0.5f));
    }

    private Text CreateLabel(string objectName, Transform parent, int fontSize, TextAnchor alignment)
    {
        GameObject labelObject = new GameObject(objectName, typeof(RectTransform), typeof(Text), typeof(Shadow));
        labelObject.transform.SetParent(parent, false);
        Text label = labelObject.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = Color.white;
        label.raycastTarget = false;
        Shadow labelShadow = labelObject.GetComponent<Shadow>();
        labelShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        labelShadow.effectDistance = new Vector2(1f, -1f);
        labelShadow.useGraphicAlpha = true;
        return label;
    }



    private Image CreateImage(string objectName, Transform parent, Vector2 size, Vector2 position)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        Image image = imageObject.GetComponent<Image>();
        image.sprite = solidSprite;
        image.raycastTarget = false;
        return image;
    }

    private void GenerateScrambledState()
    {
        System.Random random = new System.Random(randomSeed);
        int attempt = 0;

        do
        {
            scrambleQuarterTurns = 0;
            for (int row = 0; row < GridSize; row++)
            {
                for (int column = 0; column < GridSize; column++)
                {
                    if (IsFixed(row, column))
                    {
                        currentMasks[row, column] = solutionMasks[row, column];
                        continue;
                    }

                    int clockwiseTurns = 1 + random.Next(3);
                    currentMasks[row, column] = RotateMask(solutionMasks[row, column], clockwiseTurns);
                    scrambleQuarterTurns += ClockwiseDistance(currentMasks[row, column], solutionMasks[row, column]);
                }
            }

            attempt++;
        }
        while ((scrambleQuarterTurns < minimumScrambleQuarterTurns || IsReceiverReachable(currentMasks)) && attempt < 80);
    }

    private void RotateNode(int row, int column)
    {
        if (puzzleSolved || IsFixed(row, column))
            return;

        currentMasks[row, column] = RotateMask(currentMasks[row, column], 1);
        moveCount++;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        bool[,] poweredNodes = GetReachableNodes(currentMasks);
        puzzleSolved = poweredNodes[ReceiverRow, ReceiverColumn];

        for (int row = 0; row < GridSize; row++)
        {
            for (int column = 0; column < GridSize; column++)
            {
                NodeVisual visual = nodeVisuals[row, column];
                if (visual == null)
                    continue;

                ConnectorMask mask = currentMasks[row, column];
                bool isFixed = IsFixed(row, column);
                bool isSource = row == SourceRow && column == SourceColumn;
                bool isReceiver = row == ReceiverRow && column == ReceiverColumn;
                bool isPowered = poweredNodes[row, column];

                visual.background.color = isSource
                    ? sourceNodeColor
                    : isReceiver
                        ? receiverNodeColor
                        : isFixed
                            ? fixedNodeColor
                            : movableNodeColor;

                visual.fixedLabel.text = isFixed ? "▣" : string.Empty;
                visual.typeLabel.text = isSource ? "POWER" : isReceiver ? "LOAD" : GetNodeTypeLabel(mask);
                visual.typeLabel.color = isPowered ? poweredColor : isSource ? Color.white : secondaryTextColor;
                visual.center.sprite = isSource || isReceiver ? circleSprite : solidSprite;
                visual.center.color = isPowered ? poweredColor : connectorColor;
                visual.cellFrame.color = isPowered
                    ? new Color(poweredColor.r, poweredColor.g, poweredColor.b, 0.16f)
                    : isSource
                        ? new Color(0.20f, 0.85f, 0.55f, 0.18f)
                        : isReceiver
                            ? new Color(redAccentColor.r, redAccentColor.g, redAccentColor.b, 0.18f)
                            : cellFrameColor;
                visual.border.color = isFixed
                    ? new Color(fixedBorderColor.r, fixedBorderColor.g, fixedBorderColor.b, 0.16f)
                    : new Color(fixedBorderColor.r, fixedBorderColor.g, fixedBorderColor.b, 0f);
                visual.border.gameObject.SetActive(isFixed);

                for (int direction = 0; direction < DirectionMasks.Length; direction++)
                {
                    bool hasConnection = HasConnection(mask, DirectionMasks[direction]);
                    Image connector = visual.connectors[direction];
                    Image connectorGlow = visual.connectorGlows[direction];
                    connector.gameObject.SetActive(hasConnection);
                    connectorGlow.gameObject.SetActive(false);
                    connector.color = isPowered ? poweredColor : connectorColor;
                    connectorGlow.color = isPowered
                        ? new Color(poweredColor.r, poweredColor.g, poweredColor.b, 0.24f)
                        : new Color(connectorColor.r, connectorColor.g, connectorColor.b, 0.12f);
                }
            }
        }

        if (statusText != null)
        {
            statusText.text = puzzleSolved ? "ONLINE" : "OFFLINE";
            statusText.color = puzzleSolved ? poweredColor : redAccentColor;
        }

        if (moveText != null)
        {
            moveText.text = puzzleSolved
                ? "MOVES: " + moveCount + "     POWER RESTORED"
                : "MOVES: " + moveCount;
            moveText.color = secondaryTextColor;
        }

        if (titleText != null)
            titleText.color = puzzleSolved ? poweredColor : Color.white;
    }

    private bool[,] GetReachableNodes(ConnectorMask[,] masks)
    {
        bool[,] reachable = new bool[GridSize, GridSize];
        Queue<Vector2Int> pending = new Queue<Vector2Int>();
        Vector2Int source = new Vector2Int(SourceRow, SourceColumn);
        reachable[source.x, source.y] = true;
        pending.Enqueue(source);

        while (pending.Count > 0)
        {
            Vector2Int current = pending.Dequeue();
            ConnectorMask currentMask = masks[current.x, current.y];

            for (int direction = 0; direction < DirectionMasks.Length; direction++)
            {
                ConnectorMask directionMask = DirectionMasks[direction];
                Vector2Int neighbor = current + DirectionOffsets[direction];
                if (!HasConnection(currentMask, directionMask) || !IsInsideGrid(neighbor))
                    continue;

                ConnectorMask neighborMask = masks[neighbor.x, neighbor.y];
                if (!HasConnection(neighborMask, Opposite(directionMask)) || reachable[neighbor.x, neighbor.y])
                    continue;

                reachable[neighbor.x, neighbor.y] = true;
                pending.Enqueue(neighbor);
            }
        }

        return reachable;
    }

    private bool IsReceiverReachable(ConnectorMask[,] masks)
    {
        return GetReachableNodes(masks)[ReceiverRow, ReceiverColumn];
    }

    private int CountPaths(ConnectorMask[,] masks, int row, int column, int targetRow, int targetColumn, bool[,] visited)
    {
        if (row == targetRow && column == targetColumn)
            return 1;

        visited[row, column] = true;
        int pathCount = 0;
        ConnectorMask currentMask = masks[row, column];

        for (int direction = 0; direction < DirectionMasks.Length; direction++)
        {
            ConnectorMask directionMask = DirectionMasks[direction];
            Vector2Int neighbor = new Vector2Int(row, column) + DirectionOffsets[direction];
            if (!HasConnection(currentMask, directionMask) || !IsInsideGrid(neighbor) || visited[neighbor.x, neighbor.y])
                continue;

            if (!HasConnection(masks[neighbor.x, neighbor.y], Opposite(directionMask)))
                continue;

            pathCount += CountPaths(masks, neighbor.x, neighbor.y, targetRow, targetColumn, visited);
            if (pathCount > 1)
                break;
        }

        visited[row, column] = false;
        return pathCount;
    }

    private static bool IsInsideGrid(Vector2Int coordinate)
    {
        return coordinate.x >= 0 && coordinate.x < GridSize && coordinate.y >= 0 && coordinate.y < GridSize;
    }

    private bool IsFixed(int row, int column)
    {
        for (int i = 0; i < FixedNodeCoordinates.Length; i++)
        {
            Vector2Int coordinate = FixedNodeCoordinates[i];
            if (coordinate.x == row && coordinate.y == column)
                return true;
        }

        return false;
    }

    private static ConnectorMask RotateMask(ConnectorMask mask, int clockwiseTurns)
    {
        ConnectorMask rotated = mask;
        for (int turn = 0; turn < clockwiseTurns; turn++)
        {
            ConnectorMask next = ConnectorMask.None;
            if (HasConnection(rotated, ConnectorMask.Up))
                next |= ConnectorMask.Right;
            if (HasConnection(rotated, ConnectorMask.Right))
                next |= ConnectorMask.Down;
            if (HasConnection(rotated, ConnectorMask.Down))
                next |= ConnectorMask.Left;
            if (HasConnection(rotated, ConnectorMask.Left))
                next |= ConnectorMask.Up;
            rotated = next;
        }

        return rotated;
    }

    private static int ClockwiseDistance(ConnectorMask from, ConnectorMask to)
    {
        ConnectorMask candidate = from;
        for (int turns = 0; turns < 4; turns++)
        {
            if (candidate == to)
                return turns;
            candidate = RotateMask(candidate, 1);
        }

        return 0;
    }

    private static ConnectorMask Opposite(ConnectorMask direction)
    {
        switch (direction)
        {
            case ConnectorMask.Up:
                return ConnectorMask.Down;
            case ConnectorMask.Right:
                return ConnectorMask.Left;
            case ConnectorMask.Down:
                return ConnectorMask.Up;
            default:
                return ConnectorMask.Right;
        }
    }

    private static bool HasConnection(ConnectorMask mask, ConnectorMask direction)
    {
        return (mask & direction) != ConnectorMask.None;
    }

    private static string GetNodeTypeLabel(ConnectorMask mask)
    {
        int connectionCount = 0;
        if (HasConnection(mask, ConnectorMask.Up)) connectionCount++;
        if (HasConnection(mask, ConnectorMask.Right)) connectionCount++;
        if (HasConnection(mask, ConnectorMask.Down)) connectionCount++;
        if (HasConnection(mask, ConnectorMask.Left)) connectionCount++;

        if (connectionCount == 1) return "END";
        if (connectionCount == 3) return "T";
        if (connectionCount == 4) return "+";
        if (mask == (ConnectorMask.Up | ConnectorMask.Down) || mask == (ConnectorMask.Left | ConnectorMask.Right)) return "I";
        return "L";
    }

    private sealed class NodeVisual
    {
        public GameObject nodeObject;
        public Image background;
        public Image center;
        public Image border;
        public Image cellFrame;
        public Text typeLabel;
        public Text fixedLabel;
        public readonly Image[] connectors = new Image[4];
        public readonly Image[] connectorGlows = new Image[4];
        public readonly Image[] edgeLines = new Image[4];
        public readonly Image[] cornerMarkers = new Image[4];
    }

    [Flags]
    private enum ConnectorMask
    {
        None = 0,
        Up = 1,
        Right = 2,
        Down = 4,
        Left = 8
    }
}
