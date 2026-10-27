using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles energy-cell pickup, color-matched socket insertion, and reactor activation in the existing scene.
/// </summary>
public sealed class EnergyCellPuzzleController : MonoBehaviour
{
    private const string SocketPrefix = "EnergyCellSocket";
    private const string CellPrefix = "EnergyCell_";
    private const string CellCoreName = "Cylinder001";
    private const string InnerBlueCylinderName = "Cylinder.001";
    private const float TargetColliderWorldSize = 0.42f;
    private const float AimAssistRadius = 0.2f;
    private const float ScreenAimAssistRadius = 0.22f;
    private const float MinimumScale = 0.001f;
    private const float ColorSaturationThreshold = 0.15f;
    private const float MinimumVisibleProtrusionFraction = 0.4f;
    private const float MaximumVisibleProtrusionFraction = 0.6f;
    private const float DefaultVisibleProtrusionFraction = 0.5f;
    private const float FeedbackDuration = 1.1f;
    private const float ReactorRestoredFeedbackDuration = 2f;
    private const int ReactorRestoredPromptFontSize = 36;
    private const int InteractionPromptFontSize = 32;
    private const float ReactorRestoredPromptWidth = 600f;
    private const float ReactorRestoredPromptHeight = 56f;
    private const string ReactorRestoredPromptText = "Reactor Restoed";
    private const float PromptWidth = 72f;
    private const float PromptHeight = 58f;
    private const float PromptLabelWidth = 460f;
    private const float PromptLabelHeight = 58f;
    private const float PromptGap = 12f;
    private const float EmissionIntensity = 3f;
    private const float CellSocketAxisCorrectionDegrees = 90f;
    private static readonly Color ReactorOnlineColor = new Color(0.12f, 0.82f, 1f);

    [SerializeField] private float interactionDistance = 5f;
    [SerializeField] private Vector3 carriedCellLocalPosition = new Vector3(0.38f, -0.22f, 0.85f);
    [SerializeField] private Vector3 carriedCellLocalEulerAngles = Vector3.zero;
    [SerializeField] private float insertionDuration = 0.42f;
    [SerializeField, Range(MinimumVisibleProtrusionFraction, MaximumVisibleProtrusionFraction)]
    [Tooltip("Fraction of the cell's projected mesh length to leave exposed outside the socket.")]
    private float visibleProtrusionFraction = DefaultVisibleProtrusionFraction;
    [SerializeField] private PuzzleProgressionManager progressionManager;

    private readonly List<CellState> cells = new List<CellState>();
    private readonly List<SocketState> sockets = new List<SocketState>();
    private Camera playerCamera;
    private Transform cellTable;
    private Transform socketPanel;
    private Transform reactor;
    private Transform cellHoldPoint;
    [SerializeField] private Renderer innerBlueCylinderRenderer;
    private CellState carriedCell;
    private CellState targetedCell;
    private SocketState targetedSocket;
    private bool insertionInProgress;
    private string feedbackText;
    private float feedbackEndTime;
    private bool reactorActivated;
    private GUIStyle promptBoxStyle;
    private GUIStyle promptLabelStyle;
    private GUIStyle reactorRestoredPromptStyle;

    private void Start()
    {
        playerCamera = Camera.main;
        GameObject cellTableObject = GameObject.Find("EnergyCellTable");
        GameObject socketPanelObject = GameObject.Find("EnergySocketPanel");
        GameObject reactorObject = GameObject.Find("Reactor");

        if (playerCamera == null || cellTableObject == null || socketPanelObject == null || reactorObject == null)
        {
            Debug.LogError("Energy cell puzzle setup requires the tagged player camera and the existing EnergyCellTable, EnergySocketPanel, and Reactor objects.", this);
            enabled = false;
            return;
        }

        cellTable = cellTableObject.transform;
        socketPanel = socketPanelObject.transform;
        reactor = reactorObject.transform;
        cellHoldPoint = FindOrCreateHoldPoint();
        CacheSockets();
        CacheCells();

        if (cells.Count == 0 || sockets.Count == 0)
        {
            Debug.LogError("Energy cell puzzle could not find its cell or socket models under the existing scene roots.", this);
            enabled = false;
            return;
        }

        Debug.Log("Energy cell puzzle ready: " + cells.Count + " cells and " + sockets.Count + " sockets.", this);
    }

    private void Update()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                return;
            }
        }

        UpdateTarget();
        if (IsInteractPressed() && !insertionInProgress)
        {
            InteractWithTarget();
        }
    }

    private static bool IsInteractPressed()
    {
        bool pressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#if ENABLE_LEGACY_INPUT_MANAGER
        pressed |= Input.GetKeyDown(KeyCode.E);
#endif
        return pressed;
    }

    private void OnGUI()
    {
        string actionLabel = GetActionLabel();
        bool showFeedback = Time.time < feedbackEndTime && !string.IsNullOrEmpty(feedbackText);
        if (string.IsNullOrEmpty(actionLabel) && !showFeedback)
        {
            return;
        }

        EnsurePromptStyles();
        float left = Screen.width * 0.5f - PromptWidth * 0.5f;
        float top = Screen.height * 0.5f + 24f;
        if (!string.IsNullOrEmpty(actionLabel))
        {
            GUI.Box(new Rect(left, top, PromptWidth, PromptHeight), "E", promptBoxStyle);
            GUI.Label(new Rect(left + PromptWidth + PromptGap, top, PromptLabelWidth, PromptLabelHeight), actionLabel, promptLabelStyle);
        }

        if (showFeedback)
        {
            float feedbackTop = string.IsNullOrEmpty(actionLabel) ? top : top + PromptHeight + 2f;
            if (string.Equals(feedbackText, ReactorRestoredPromptText, StringComparison.Ordinal))
            {
                Rect completionPromptRect = new Rect(
                    Screen.width * 0.5f - ReactorRestoredPromptWidth * 0.5f,
                    feedbackTop,
                    ReactorRestoredPromptWidth,
                    ReactorRestoredPromptHeight);
                GUI.Label(completionPromptRect, feedbackText, reactorRestoredPromptStyle);
            }
            else
            {
                GUI.Label(new Rect(left, feedbackTop, PromptLabelWidth + PromptWidth, PromptLabelHeight), feedbackText, promptLabelStyle);
            }
        }
    }

    private Transform FindOrCreateHoldPoint()
    {
        Transform holdPoint = playerCamera.transform.Find("EnergyCellHoldPoint");
        if (holdPoint != null)
        {
            return holdPoint;
        }

        GameObject holdPointObject = new GameObject("EnergyCellHoldPoint");
        holdPoint = holdPointObject.transform;
        holdPoint.SetParent(playerCamera.transform, false);
        holdPoint.localPosition = carriedCellLocalPosition;
        holdPoint.localRotation = Quaternion.Euler(carriedCellLocalEulerAngles);
        holdPoint.localScale = Vector3.one;
        Debug.LogWarning("Created a runtime energy-cell hold point. Add EnergyCellHoldPoint under the player camera to persist editor placement.", playerCamera);
        return holdPoint;
    }

    private void CacheSockets()
    {
        for (int index = 0; index < socketPanel.childCount; index++)
        {
            Transform socketTransform = socketPanel.GetChild(index);
            if (!socketTransform.name.StartsWith(SocketPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            EnergyColor socketColor;
            if (!TryGetSocketColor(socketTransform.name, out socketColor))
            {
                Debug.LogWarning("Ignoring unrecognized energy socket " + socketTransform.name + ".", socketTransform);
                continue;
            }

            Transform insertionPoint = socketTransform.Find("InsertionPoint");
            if (insertionPoint == null)
            {
                GameObject insertionPointObject = new GameObject("InsertionPoint");
                insertionPoint = insertionPointObject.transform;
                insertionPoint.SetParent(socketTransform, false);
                Debug.LogWarning("Created a runtime insertion point for " + socketTransform.name + ". Add an InsertionPoint child in the scene to persist editor placement.", socketTransform);
            }

            insertionPoint.position = socketTransform.position;
            insertionPoint.rotation = GetCellInsertionRotation(GetPlayerFacingSocketAxis(socketTransform));
            EnsureTargetCollider(socketTransform);
            sockets.Add(new SocketState(socketTransform, insertionPoint, socketColor));
        }
    }

    private void CacheCells()
    {
        for (int index = 0; index < cellTable.childCount; index++)
        {
            Transform cellTransform = cellTable.GetChild(index);
            if (!cellTransform.name.StartsWith(CellPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            EnergyColor cellColor = ReadCellColor(cellTransform);
            BoxCollider pickupCollider = EnsureTargetCollider(cellTransform);
            cells.Add(new CellState(cellTransform, cellColor, pickupCollider));
            Debug.Log("Mapped " + cellColor + " energy cell: " + cellTransform.name + ".", cellTransform);
        }
    }

    private BoxCollider EnsureTargetCollider(Transform target)
    {
        BoxCollider targetCollider = target.GetComponent<BoxCollider>();
        if (targetCollider == null)
        {
            targetCollider = target.gameObject.AddComponent<BoxCollider>();
        }

        Vector3 scale = target.lossyScale;
        targetCollider.center = Vector3.zero;
        targetCollider.size = new Vector3(
            TargetColliderWorldSize / Mathf.Max(Mathf.Abs(scale.x), MinimumScale),
            TargetColliderWorldSize / Mathf.Max(Mathf.Abs(scale.y), MinimumScale),
            TargetColliderWorldSize / Mathf.Max(Mathf.Abs(scale.z), MinimumScale));
        targetCollider.isTrigger = true;
        targetCollider.enabled = true;
        return targetCollider;
    }

    private EnergyColor ReadCellColor(Transform cellTransform)
    {
        Transform coreTransform = cellTransform.Find(CellCoreName);
        Renderer coreRenderer = coreTransform == null ? null : coreTransform.GetComponent<Renderer>();
        if (coreRenderer != null)
        {
            Material[] materials = coreRenderer.sharedMaterials;
            for (int index = 0; index < materials.Length; index++)
            {
                Material material = materials[index];
                if (material == null || !material.HasProperty("_EmissionColor"))
                {
                    continue;
                }

                Color emissionColor = material.GetColor("_EmissionColor");
                float maximum = Mathf.Max(emissionColor.r, Mathf.Max(emissionColor.g, emissionColor.b));
                float minimum = Mathf.Min(emissionColor.r, Mathf.Min(emissionColor.g, emissionColor.b));
                if (maximum - minimum < ColorSaturationThreshold)
                {
                    return EnergyColor.White;
                }

                if (emissionColor.r > ColorSaturationThreshold && emissionColor.g > ColorSaturationThreshold && emissionColor.b < emissionColor.g)
                {
                    return EnergyColor.Yellow;
                }

                if (emissionColor.r >= emissionColor.g && emissionColor.r >= emissionColor.b)
                {
                    return EnergyColor.Red;
                }

                return emissionColor.g >= emissionColor.b ? EnergyColor.Green : EnergyColor.Blue;
            }
        }

        Debug.LogWarning("Could not read an emissive color from " + cellTransform.name + "; assigning White as a safe fallback.", cellTransform);
        return EnergyColor.White;
    }

    private static bool TryGetSocketColor(string socketName, out EnergyColor color)
    {
        if (string.Equals(socketName, SocketPrefix, StringComparison.Ordinal))
        {
            color = EnergyColor.Green;
            return true;
        }

        string suffix = socketName.Substring(SocketPrefix.Length).TrimStart('_');
        return Enum.TryParse(suffix, true, out color);
    }

    private Quaternion GetHorizontalCellInsertionRotation()
    {
        Quaternion socketLocalZAlignment = Quaternion.LookRotation(socketPanel.forward, socketPanel.up);
        return socketLocalZAlignment * Quaternion.Euler(0f, CellSocketAxisCorrectionDegrees, 0f);
    }

    private Vector3 GetPlayerFacingSocketAxis(Transform socketTransform)
    {
        Vector3 insertionAxis = (GetHorizontalCellInsertionRotation() * Vector3.forward).normalized;
        Vector3 directionToPlayer = playerCamera.transform.position - socketTransform.position;
        if (Vector3.Dot(directionToPlayer, insertionAxis) < 0f)
        {
            insertionAxis = -insertionAxis;
        }

        return insertionAxis;
    }

    private Quaternion GetCellInsertionRotation(Vector3 insertionDirection)
    {
        Vector3 cellUp = Vector3.ProjectOnPlane(socketPanel.up, insertionDirection).normalized;
        if (cellUp.sqrMagnitude <= MinimumScale)
        {
            cellUp = Vector3.ProjectOnPlane(socketPanel.right, insertionDirection).normalized;
        }

        return Quaternion.LookRotation(insertionDirection, cellUp);
    }


    private void UpdateTarget()
    {
        targetedCell = null;
        targetedSocket = null;

        FindScreenSpaceFallbackTarget();
        if (targetedCell != null || targetedSocket != null)
        {
            return;
        }

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] hits = Physics.SphereCastAll(ray, AimAssistRadius, interactionDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        for (int index = 0; index < hits.Length; index++)
        {
            Transform hitTransform = hits[index].collider.transform;
            CellState hitCell = FindCell(hitTransform);
            if (hitCell != null)
            {
                if (carriedCell == null && hitCell.isAvailable)
                {
                    targetedCell = hitCell;
                }

                return;
            }

            SocketState hitSocket = FindSocket(hitTransform);
            if (hitSocket != null)
            {
                if (carriedCell != null && !hitSocket.isOccupied && !insertionInProgress)
                {
                    targetedSocket = hitSocket;
                }

                return;
            }

            if (IsPuzzleSupportGeometry(hitTransform) || IsPlayerGeometry(hitTransform))
            {
                continue;
            }

            return;
        }

        FindScreenSpaceFallbackTarget();
    }

    private void FindScreenSpaceFallbackTarget()
    {
        float bestScreenDistance = ScreenAimAssistRadius * ScreenAimAssistRadius;

        if (carriedCell == null)
        {
            for (int index = 0; index < cells.Count; index++)
            {
                CellState cell = cells[index];
                if (!cell.isAvailable)
                {
                    continue;
                }

                float screenDistance = GetScreenDistanceSquared(cell.transform);
                if (screenDistance < bestScreenDistance && HasClearLineOfSight(cell.transform))
                {
                    targetedCell = cell;
                    targetedSocket = null;
                    bestScreenDistance = screenDistance;
                }
            }

            return;
        }

        for (int index = 0; index < sockets.Count; index++)
        {
            SocketState socket = sockets[index];
            if (socket.isOccupied)
            {
                continue;
            }

            float screenDistance = GetScreenDistanceSquared(socket.transform);
            if (screenDistance < bestScreenDistance && HasClearLineOfSight(socket.transform))
            {
                targetedSocket = socket;
                targetedCell = null;
                bestScreenDistance = screenDistance;
            }
        }
    }

    private float GetScreenDistanceSquared(Transform target)
    {
        Vector3 viewportPoint = playerCamera.WorldToViewportPoint(target.position);
        if (viewportPoint.z <= 0f || Vector3.Distance(playerCamera.transform.position, target.position) > interactionDistance)
        {
            return float.PositiveInfinity;
        }

        Vector2 screenOffset = new Vector2(viewportPoint.x - 0.5f, viewportPoint.y - 0.5f);
        return screenOffset.sqrMagnitude;
    }

    private bool HasClearLineOfSight(Transform target)
    {
        Vector3 offset = target.position - playerCamera.transform.position;
        float distance = offset.magnitude;
        if (distance <= MinimumScale)
        {
            return true;
        }

        RaycastHit[] hits = Physics.RaycastAll(playerCamera.transform.position, offset / distance, distance + 0.02f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
        for (int index = 0; index < hits.Length; index++)
        {
            Transform hitTransform = hits[index].collider.transform;
            if (hitTransform == target || hitTransform.IsChildOf(target))
            {
                return true;
            }

            if (IsPuzzleSupportGeometry(hitTransform) || IsPlayerGeometry(hitTransform))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private CellState FindCell(Transform candidate)
    {
        for (int index = 0; index < cells.Count; index++)
        {
            Transform cellTransform = cells[index].transform;
            if (candidate == cellTransform || candidate.IsChildOf(cellTransform))
            {
                return cells[index];
            }
        }

        return null;
    }

    private SocketState FindSocket(Transform candidate)
    {
        for (int index = 0; index < sockets.Count; index++)
        {
            Transform socketTransform = sockets[index].transform;
            if (candidate == socketTransform || candidate.IsChildOf(socketTransform))
            {
                return sockets[index];
            }
        }

        return null;
    }

    private bool IsPuzzleSupportGeometry(Transform candidate)
    {
        return candidate == cellTable || candidate.IsChildOf(cellTable) || candidate == socketPanel || candidate.IsChildOf(socketPanel);
    }

    private bool IsPlayerGeometry(Transform candidate)
    {
        Transform playerRoot = playerCamera.transform.root;
        return candidate == playerRoot || candidate.IsChildOf(playerRoot);
    }

    private void InteractWithTarget()
    {
        if (carriedCell == null && targetedCell != null)
        {
            PickUpCell(targetedCell);
            return;
        }

        if (carriedCell != null && targetedSocket != null)
        {
            InsertCarriedCell(targetedSocket);
        }
    }

    private void PickUpCell(CellState cell)
    {
        if (cell == null || !cell.isAvailable || carriedCell != null)
        {
            return;
        }

        cell.isAvailable = false;
        cell.pickupCollider.enabled = false;
        cell.transform.SetParent(cellHoldPoint, false);
        cell.transform.localPosition = Vector3.zero;
        cell.transform.localRotation = Quaternion.identity;
        cell.transform.localScale = cell.originalLocalScale;
        carriedCell = cell;
        feedbackText = string.Empty;
        Debug.Log("Picked up " + cell.color + " energy cell.", cell.transform);
    }

    private void InsertCarriedCell(SocketState socket)
    {
        if (socket == null || socket.isOccupied || carriedCell == null || insertionInProgress)
        {
            return;
        }

        if (carriedCell.color != socket.requiredColor)
        {
            feedbackText = "Color mismatch: " + carriedCell.color + " cell needs its matching socket.";
            feedbackEndTime = Time.time + FeedbackDuration;
            Debug.Log("Energy cell color mismatch: " + carriedCell.color + " cell cannot enter " + socket.requiredColor + " socket.");
            return;
        }

        socket.isOccupied = true;
        carriedCell.pickupCollider.enabled = false;
        insertionInProgress = true;
        StartCoroutine(AnimateInsertion(carriedCell, socket));
    }

    private IEnumerator AnimateInsertion(CellState cell, SocketState socket)
    {
        Transform cellTransform = cell.transform;
        Vector3 startPosition = cellTransform.position;
        Quaternion startRotation = cellTransform.rotation;
        Vector3 insertionDirection = GetPlayerFacingSocketAxis(socket.transform);
        Quaternion targetRotation = GetCellInsertionRotation(insertionDirection);
        Vector3 targetPosition = CalculateInsertionTargetPosition(cellTransform, socket, targetRotation, insertionDirection);
        float duration = Mathf.Max(insertionDuration, MinimumScale);
        float elapsed = 0f;

        cellTransform.SetParent(null, true);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);
            cellTransform.position = Vector3.Lerp(startPosition, targetPosition, easedTime);
            cellTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, easedTime);
            yield return null;
        }

        cellTransform.SetParent(socket.transform, true);
        cellTransform.position = targetPosition;
        cellTransform.rotation = targetRotation;
        cell.isInserted = true;
        cell.isAvailable = false;
        carriedCell = null;
        insertionInProgress = false;
        feedbackText = cell.color + " cell inserted.";
        feedbackEndTime = Time.time + FeedbackDuration;
        Debug.Log("Inserted " + cell.color + " energy cell into its matching socket.", cellTransform);

        if (CountOccupiedSockets() == sockets.Count && !reactorActivated)
        {
            ActivateReactor();
        }
    }

    private Vector3 CalculateInsertionTargetPosition(Transform cellTransform, SocketState socket, Quaternion targetRotation, Vector3 insertionDirection)
    {
        float cellMinimumProjection;
        float cellMaximumProjection;
        if (!TryGetMeshProjectionRange(cellTransform, targetRotation, insertionDirection, out cellMinimumProjection, out cellMaximumProjection))
        {
            return socket.insertionPoint.position;
        }

        float cellProjectedLength = cellMaximumProjection - cellMinimumProjection;
        if (cellProjectedLength <= MinimumScale)
        {
            return socket.insertionPoint.position;
        }

        float socketFrontProjection;
        if (!TryGetFrontSurfaceProjection(socket.transform, insertionDirection, out socketFrontProjection))
        {
            socketFrontProjection = Vector3.Dot(socket.insertionPoint.position, insertionDirection);
        }

        float exposedFraction = Mathf.Clamp(visibleProtrusionFraction, MinimumVisibleProtrusionFraction, MaximumVisibleProtrusionFraction);
        float targetRootProjection = socketFrontProjection + cellProjectedLength * exposedFraction - cellMaximumProjection;
        Vector3 insertionPointPosition = socket.insertionPoint.position;
        float insertionPointProjection = Vector3.Dot(insertionPointPosition, insertionDirection);
        return insertionPointPosition + insertionDirection * (targetRootProjection - insertionPointProjection);
    }

    private static bool TryGetMeshProjectionRange(Transform root, Quaternion targetRotation, Vector3 insertionDirection, out float minimumProjection, out float maximumProjection)
    {
        minimumProjection = float.PositiveInfinity;
        maximumProjection = float.NegativeInfinity;
        bool foundMesh = false;
        MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);

        for (int filterIndex = 0; filterIndex < meshFilters.Length; filterIndex++)
        {
            MeshFilter meshFilter = meshFilters[filterIndex];
            Mesh mesh = meshFilter.sharedMesh;
            Renderer meshRenderer = meshFilter.GetComponent<Renderer>();
            if (mesh == null || meshRenderer == null || !meshRenderer.enabled || !meshFilter.gameObject.activeInHierarchy)
            {
                continue;
            }

            Bounds meshBounds = mesh.bounds;
            for (int cornerIndex = 0; cornerIndex < 8; cornerIndex++)
            {
                Vector3 meshLocalCorner = GetBoundsCorner(meshBounds, cornerIndex);
                Vector3 rootLocalCorner = root.InverseTransformPoint(meshFilter.transform.TransformPoint(meshLocalCorner));
                Vector3 targetRelativeCorner = targetRotation * Vector3.Scale(root.lossyScale, rootLocalCorner);
                float projection = Vector3.Dot(targetRelativeCorner, insertionDirection);
                minimumProjection = Mathf.Min(minimumProjection, projection);
                maximumProjection = Mathf.Max(maximumProjection, projection);
                foundMesh = true;
            }
        }

        return foundMesh;
    }

    private static bool TryGetFrontSurfaceProjection(Transform socketRoot, Vector3 insertionDirection, out float frontSurfaceProjection)
    {
        frontSurfaceProjection = float.NegativeInfinity;
        bool foundMesh = false;
        MeshFilter[] meshFilters = socketRoot.GetComponentsInChildren<MeshFilter>(true);

        for (int filterIndex = 0; filterIndex < meshFilters.Length; filterIndex++)
        {
            MeshFilter meshFilter = meshFilters[filterIndex];
            Mesh mesh = meshFilter.sharedMesh;
            Renderer meshRenderer = meshFilter.GetComponent<Renderer>();
            if (mesh == null || meshRenderer == null || !meshRenderer.enabled || !meshFilter.gameObject.activeInHierarchy)
            {
                continue;
            }

            Bounds meshBounds = mesh.bounds;
            for (int cornerIndex = 0; cornerIndex < 8; cornerIndex++)
            {
                Vector3 worldCorner = meshFilter.transform.TransformPoint(GetBoundsCorner(meshBounds, cornerIndex));
                float projection = Vector3.Dot(worldCorner, insertionDirection);
                frontSurfaceProjection = Mathf.Max(frontSurfaceProjection, projection);
                foundMesh = true;
            }
        }

        return foundMesh;
    }

    private static Vector3 GetBoundsCorner(Bounds bounds, int cornerIndex)
    {
        Vector3 signs = new Vector3(
            (cornerIndex & 1) == 0 ? -1f : 1f,
            (cornerIndex & 2) == 0 ? -1f : 1f,
            (cornerIndex & 4) == 0 ? -1f : 1f);
        return bounds.center + Vector3.Scale(bounds.extents, signs);
    }

    private int CountOccupiedSockets()
    {
        int occupiedCount = 0;
        for (int index = 0; index < sockets.Count; index++)
        {
            if (sockets[index].isOccupied)
            {
                occupiedCount++;
            }
        }

        return occupiedCount;
    }

    private void ActivateReactor()
    {
        if (reactor == null)
        {
            return;
        }

        if (innerBlueCylinderRenderer == null)
        {
            Transform innerBlueCylinder = reactor.Find(InnerBlueCylinderName);
            if (innerBlueCylinder != null)
            {
                innerBlueCylinderRenderer = innerBlueCylinder.GetComponent<Renderer>();
            }
        }

        if (innerBlueCylinderRenderer != null)
        {
            Material activationMaterial = innerBlueCylinderRenderer.material;
            if (activationMaterial != null && activationMaterial.HasProperty("_EmissionColor"))
            {
                if (activationMaterial.HasProperty("_EmissionMap") && activationMaterial.GetTexture("_EmissionMap") == null && activationMaterial.HasProperty("_BaseMap"))
                {
                    Texture emissionMask = activationMaterial.GetTexture("_BaseMap");
                    if (emissionMask != null)
                    {
                        activationMaterial.SetTexture("_EmissionMap", emissionMask);
                    }
                }

                activationMaterial.EnableKeyword("_EMISSION");
                activationMaterial.SetColor("_EmissionColor", ReactorOnlineColor * EmissionIntensity);
            }
        }
        else
        {
            Debug.LogWarning("Could not find the reactor's inner blue cylinder renderer. Assign it to innerBlueCylinderRenderer on EnergyCellPuzzleController.", reactor);
        }

        reactorActivated = true;
        feedbackText = ReactorRestoredPromptText;
        feedbackEndTime = Time.time + ReactorRestoredFeedbackDuration;
        if (progressionManager != null)
        {
            progressionManager.ReportPuzzle3Completed();
        }

        Debug.Log("All energy cells inserted. Reactor activated.", reactor);
    }

    private string GetActionLabel()
    {
        if (targetedCell != null && carriedCell == null)
        {
            return "Pick up " + targetedCell.color + " energy cell";
        }

        if (targetedSocket != null && carriedCell != null)
        {
            return "Insert " + carriedCell.color + " cell";
        }

        return string.Empty;
    }

    private void EnsurePromptStyles()
    {
        if (promptBoxStyle != null && promptLabelStyle != null && reactorRestoredPromptStyle != null)
        {
            return;
        }

        promptBoxStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = InteractionPromptFontSize,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        promptLabelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = InteractionPromptFontSize,
            normal = { textColor = Color.white }
        };
        reactorRestoredPromptStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = ReactorRestoredPromptFontSize,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
    }

    private enum EnergyColor
    {
        Red,
        Blue,
        Green,
        White,
        Yellow
    }

    private sealed class CellState
    {
        public readonly Transform transform;
        public readonly EnergyColor color;
        public readonly BoxCollider pickupCollider;
        public readonly Vector3 originalLocalScale;
        public bool isAvailable = true;
        public bool isInserted;

        public CellState(Transform cellTransform, EnergyColor cellColor, BoxCollider collider)
        {
            transform = cellTransform;
            color = cellColor;
            pickupCollider = collider;
            originalLocalScale = cellTransform.localScale;
        }
    }

    private sealed class SocketState
    {
        public readonly Transform transform;
        public readonly Transform insertionPoint;
        public readonly EnergyColor requiredColor;
        public bool isOccupied;

        public SocketState(Transform socketTransform, Transform socketInsertionPoint, EnergyColor socketColor)
        {
            transform = socketTransform;
            insertionPoint = socketInsertionPoint;
            requiredColor = socketColor;
        }
    }
}
