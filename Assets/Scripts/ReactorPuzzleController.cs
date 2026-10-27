using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Provides physics-based grabbing, keyed connector alignment, insertion feedback, and reactor power-up behavior.
/// </summary>
public sealed class ReactorPuzzleController : MonoBehaviour
{
    private const string PlugPrefix = "Plug-";
    private const string SocketPrefix = "Socket-";
    private const float PlugTipOffset = 0.24f;
    private const float SocketFaceOffset = 0.26f;
    private const float MagneticRange = 0.95f;

    [SerializeField] private float grabDistance = 3.6f;
    [SerializeField] private float grabHoldDistance = 1.35f;
    [SerializeField] private float insertionDistance = 0.095f;
    [SerializeField] private float insertionAngle = 17f;

    private Camera playerCamera;
    private Rigidbody heldAnchorBody;
    private Rigidbody heldBody;
    private SpringJoint heldJoint;
    private Transform[] plugs = new Transform[0];
    private Transform[] sockets = new Transform[0];
    private Light[] emergencyLights = new Light[0];
    private readonly HashSet<string> connectedCodes = new HashSet<string>();
    private readonly Dictionary<Transform, float> warningEndTimes = new Dictionary<Transform, float>();
    private float nextFlickerTime;

    private void Start()
    {
        playerCamera = Camera.main;
        CachePuzzleObjects();
        CreateGrabAnchor();
        CreateAmbientSmoke();
        CacheEmergencyLights();
    }

    private void Update()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera != null && heldAnchorBody == null)
            {
                CreateGrabAnchor();
            }
        }

        if (playerCamera != null)
        {
            if (Input.GetMouseButtonDown(0))
            {
                TryGrabConnector();
            }

            if (Input.GetMouseButtonUp(0))
            {
                ReleaseConnector();
            }
        }

        UpdateWarningLights();
        FlickerEmergencyLights();
    }

    private void FixedUpdate()
    {
        if (playerCamera == null)
        {
            return;
        }

        if (heldAnchorBody != null)
        {
            heldAnchorBody.position = playerCamera.transform.position + playerCamera.transform.forward * grabHoldDistance;
            heldAnchorBody.rotation = playerCamera.transform.rotation;
        }

        for (int plugIndex = 0; plugIndex < plugs.Length; plugIndex++)
        {
            Transform plug = plugs[plugIndex];
            if (plug == null || !plug.gameObject.activeInHierarchy)
            {
                continue;
            }

            Rigidbody body = plug.GetComponent<Rigidbody>();
            if (body == null || connectedCodes.Contains(GetCode(plug.name)))
            {
                continue;
            }

            Vector3 tipPosition = plug.position + plug.forward * PlugTipOffset;
            string plugCode = GetCode(plug.name);

            for (int socketIndex = 0; socketIndex < sockets.Length; socketIndex++)
            {
                Transform socket = sockets[socketIndex];
                if (socket == null || !socket.gameObject.activeInHierarchy)
                {
                    continue;
                }

                string socketCode = GetCode(socket.name);
                Vector3 targetTipPosition = socket.position + socket.forward * SocketFaceOffset;
                float tipDistance = Vector3.Distance(tipPosition, targetTipPosition);
                bool isCorrectSocket = plugCode == socketCode;

                if (isCorrectSocket && tipDistance < MagneticRange)
                {
                    ApplyMagneticAssistance(body, plug, socket, tipPosition, targetTipPosition);
                    float angle = Vector3.Angle(plug.forward, -socket.forward);
                    if (tipDistance <= insertionDistance && angle <= insertionAngle && body.linearVelocity.magnitude < 2.4f)
                    {
                        LockConnector(body, plug, socket, plugCode);
                        break;
                    }
                }
                else if (!isCorrectSocket && tipDistance < 0.18f)
                {
                    RejectWrongSocket(body, socket);
                }
            }
        }
    }

    private void CachePuzzleObjects()
    {
        Transform[] allTransforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        List<Transform> foundPlugs = new List<Transform>();
        List<Transform> foundSockets = new List<Transform>();

        for (int index = 0; index < allTransforms.Length; index++)
        {
            Transform item = allTransforms[index];
            if (item.name.StartsWith(PlugPrefix))
            {
                foundPlugs.Add(item);
            }
            else if (item.name.StartsWith(SocketPrefix))
            {
                foundSockets.Add(item);
            }
        }

        plugs = foundPlugs.ToArray();
        sockets = foundSockets.ToArray();
    }

    private void CreateGrabAnchor()
    {
        GameObject anchor = new GameObject("ReactorGrabAnchor");
        anchor.transform.SetParent(playerCamera.transform, false);
        anchor.transform.localPosition = new Vector3(0f, 0f, grabHoldDistance);
        heldAnchorBody = anchor.AddComponent<Rigidbody>();
        heldAnchorBody.isKinematic = true;
        heldAnchorBody.useGravity = false;
    }

    private void TryGrabConnector()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, grabDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        Rigidbody targetBody = hit.rigidbody;
        if (targetBody == null || !targetBody.gameObject.name.StartsWith(PlugPrefix) || connectedCodes.Contains(GetCode(targetBody.gameObject.name)))
        {
            return;
        }

        ReleaseConnector();
        heldBody = targetBody;
        heldJoint = heldBody.gameObject.AddComponent<SpringJoint>();
        heldJoint.connectedBody = heldAnchorBody;
        heldJoint.autoConfigureConnectedAnchor = false;
        heldJoint.anchor = heldBody.transform.InverseTransformPoint(hit.point);
        heldJoint.connectedAnchor = Vector3.zero;
        heldJoint.spring = 115f;
        heldJoint.damper = 17f;
        heldJoint.minDistance = 0f;
        heldJoint.maxDistance = 0.025f;
        heldJoint.tolerance = 0.025f;
        heldJoint.enableCollision = true;
    }

    private void ReleaseConnector()
    {
        if (heldJoint != null)
        {
            Destroy(heldJoint);
        }

        heldJoint = null;
        heldBody = null;
    }

    private static void ApplyMagneticAssistance(Rigidbody body, Transform plug, Transform socket, Vector3 tipPosition, Vector3 targetTipPosition)
    {
        Vector3 desiredForward = -socket.forward;
        Vector3 positionError = targetTipPosition - tipPosition;
        body.AddForce(positionError * 5.5f - body.linearVelocity * 1.1f, ForceMode.Acceleration);

        Quaternion desiredRotation = Quaternion.LookRotation(desiredForward, Vector3.up);
        Quaternion rotationDelta = desiredRotation * Quaternion.Inverse(plug.rotation);
        rotationDelta.ToAngleAxis(out float angleDegrees, out Vector3 rotationAxis);
        if (angleDegrees > 180f)
        {
            angleDegrees -= 360f;
        }

        if (rotationAxis.sqrMagnitude > 0.001f)
        {
            Vector3 angularCorrection = rotationAxis.normalized * angleDegrees * Mathf.Deg2Rad * 5f;
            body.AddTorque(angularCorrection - body.angularVelocity * 0.8f, ForceMode.Acceleration);
        }
    }

    private void RejectWrongSocket(Rigidbody body, Transform socket)
    {
        body.AddForce(socket.forward * 6.5f, ForceMode.Acceleration);
        Light statusLight = FindStatusLight(socket);
        if (statusLight != null)
        {
            statusLight.color = new Color(1f, 0.52f, 0.12f);
            bool shouldSpark = !warningEndTimes.TryGetValue(socket, out float warningEndTime) || Time.time > warningEndTime + 0.04f;
            warningEndTimes[socket] = Time.time + 0.28f;
            if (shouldSpark)
            {
                SpawnConnectionSparks(socket.position + socket.forward * 0.12f);
            }
        }
    }

    private void LockConnector(Rigidbody body, Transform plug, Transform socket, string code)
    {
        ReleaseConnectorIfHeld(body);

        Quaternion targetRotation = Quaternion.LookRotation(-socket.forward, Vector3.up);
        Vector3 targetPosition = socket.position + socket.forward * (PlugTipOffset + SocketFaceOffset);
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.transform.SetPositionAndRotation(targetPosition, targetRotation);
        ConfigurableJoint tether = body.GetComponent<ConfigurableJoint>();
        if (tether != null)
        {
            Destroy(tether);
        }

        body.isKinematic = true;
        connectedCodes.Add(code);
        Light statusLight = FindStatusLight(socket);
        if (statusLight != null)
        {
            statusLight.color = new Color(0.18f, 1f, 0.42f);
            statusLight.intensity = 3.5f;
        }

        SpawnConnectionSparks(socket.position);
        if (connectedCodes.Count >= 4)
        {
            PowerUpReactor();
        }

        Debug.Log("Reactor connector locked: " + code);
    }

    private void ReleaseConnectorIfHeld(Rigidbody body)
    {
        if (heldBody == body)
        {
            ReleaseConnector();
        }
    }

    private static Light FindStatusLight(Transform socket)
    {
        Transform lightTransform = socket.Find("StatusLight");
        return lightTransform == null ? null : lightTransform.GetComponent<Light>();
    }

    private static string GetCode(string objectName)
    {
        int separatorIndex = objectName.IndexOf('-');
        return separatorIndex < 0 ? objectName : objectName.Substring(separatorIndex + 1);
    }

    private void UpdateWarningLights()
    {
        List<Transform> expiredWarnings = new List<Transform>();
        foreach (KeyValuePair<Transform, float> warning in warningEndTimes)
        {
            if (warning.Key == null || Time.time >= warning.Value)
            {
                if (warning.Key != null && !connectedCodes.Contains(GetCode(warning.Key.name)))
                {
                    Light statusLight = FindStatusLight(warning.Key);
                    if (statusLight != null)
                    {
                        statusLight.color = new Color(1f, 0.08f, 0.035f);
                    }
                }

                expiredWarnings.Add(warning.Key);
            }
        }

        for (int index = 0; index < expiredWarnings.Count; index++)
        {
            warningEndTimes.Remove(expiredWarnings[index]);
        }
    }

    private void CacheEmergencyLights()
    {
        Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        List<Light> foundLights = new List<Light>();
        for (int index = 0; index < allLights.Length; index++)
        {
            if (allLights[index].name.StartsWith("EmergencyLamp"))
            {
                foundLights.Add(allLights[index]);
            }
        }

        emergencyLights = foundLights.ToArray();
    }

    private void FlickerEmergencyLights()
    {
        if (Time.time < nextFlickerTime || connectedCodes.Count >= 4)
        {
            return;
        }

        nextFlickerTime = Time.time + Random.Range(0.045f, 0.2f);
        for (int index = 0; index < emergencyLights.Length; index++)
        {
            if (emergencyLights[index] != null)
            {
                emergencyLights[index].intensity = Random.Range(0.55f, 2.4f);
            }
        }
    }

    private void PowerUpReactor()
    {
        GameObject reactorCore = GameObject.Find("ReactorCore");
        if (reactorCore != null)
        {
            Renderer coreRenderer = reactorCore.GetComponent<Renderer>();
            if (coreRenderer != null)
            {
                Material coreMaterial = coreRenderer.material;
                coreMaterial.EnableKeyword("_EMISSION");
                coreMaterial.SetColor("_EmissionColor", new Color(0.15f, 0.85f, 1f) * 3f);
            }
        }

        for (int index = 0; index < emergencyLights.Length; index++)
        {
            if (emergencyLights[index] != null)
            {
                emergencyLights[index].color = new Color(0.16f, 0.85f, 1f);
                emergencyLights[index].intensity = 2.6f;
            }
        }
    }

    private void CreateAmbientSmoke()
    {
        Transform[] allTransforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        for (int index = 0; index < allTransforms.Length; index++)
        {
            Transform vent = allTransforms[index];
            if (!vent.name.StartsWith("SmokeVent"))
            {
                continue;
            }

            GameObject smokeObject = new GameObject("AmbientSmoke");
            smokeObject.transform.position = vent.position;
            ParticleSystem smoke = smokeObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = smoke.main;
            main.loop = true;
            main.startLifetime = 3.5f;
            main.startSpeed = 0.22f;
            main.startSize = 0.48f;
            main.startColor = new Color(0.28f, 0.31f, 0.34f, 0.24f);
            ParticleSystem.EmissionModule emission = smoke.emission;
            emission.rateOverTime = 1.25f;
            ParticleSystem.ShapeModule shape = smoke.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 13f;
            shape.radius = 0.18f;
        }
    }

    private static void SpawnConnectionSparks(Vector3 position)
    {
        GameObject sparkObject = new GameObject("ConnectorSparks");
        sparkObject.transform.position = position;
        ParticleSystem sparks = sparkObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = sparks.main;
        main.loop = false;
        main.duration = 0.28f;
        main.startLifetime = 0.32f;
        main.startSpeed = 1.8f;
        main.startSize = 0.045f;
        main.startColor = new Color(1f, 0.64f, 0.18f);
        ParticleSystem.EmissionModule emission = sparks.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 14) });
        ParticleSystem.ShapeModule shape = sparks.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.08f;
        sparks.Play();
        Destroy(sparkObject, 1.2f);
    }
}
