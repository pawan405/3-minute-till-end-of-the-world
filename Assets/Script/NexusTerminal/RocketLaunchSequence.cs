using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RocketLaunchSequence : MonoBehaviour
{
    private const float DefaultCountdownStep = 1f;
    private const float DefaultLaunchDuration = 8f;
    private const float DefaultShakeDuration = 1.25f;

    [Header("Progression")]
    [SerializeField] private LevelFlowController levelFlow;
    [SerializeField] private LevelTransition levelTransition;
    [SerializeField] private TerminalUI terminalUI;
    [SerializeField] private GameObject launchOverlay;
    [SerializeField] private TMP_Text countdownText;

    [Header("Rocket Presentation")]
    [SerializeField] private Transform rocket;
    [SerializeField] private Animator rocketAnimator;
    [SerializeField] private Transform launchStartMarker;
    [SerializeField] private Transform launchExitMarker;
    [SerializeField] private RuntimeAnimatorController flightController;
    [SerializeField] private GameObject rocketTrailPrefab;
    [SerializeField] private GameObject launchEffectPrefab;
    [SerializeField] private ExplosionEffect explosionEffect;

    [Header("Camera and Player Lock")]
    [SerializeField] private Camera launchCamera;
    [SerializeField] private Behaviour[] behavioursToDisable;
    [SerializeField] private float cameraFollowHeight = 3f;
    [SerializeField] private float cameraFollowDistance = 10f;

    [Header("Timing")]
    [SerializeField, Min(0.1f)] private float countdownStepDuration = DefaultCountdownStep;
    [SerializeField, Min(1f)] private float launchDuration = DefaultLaunchDuration;
    [SerializeField, Min(0.1f)] private float preLaunchShakeDuration = DefaultShakeDuration;

    private Coroutine launchRoutine;
    private GameObject trailInstance;
    private Vector3 rocketStartPosition;
    private Quaternion rocketStartRotation;
    private bool[] behaviourStates;
    private bool[] behaviourStatesCached;

    public bool IsRunning => launchRoutine != null;

    /// <summary>
    /// Starts the guarded warning, countdown, and rocket launch presentation.
    /// </summary>
    public void PlayLaunch()
    {
        if (IsRunning || levelFlow == null || !levelFlow.ReactorAuthorizationAccepted)
        {
            return;
        }

        launchRoutine = StartCoroutine(LaunchRoutine());
    }

    /// <summary>
    /// Cancels the active cinematic and restores player and camera behaviours.
    /// </summary>
    public void CancelLaunch()
    {
        if (launchRoutine != null)
        {
            StopCoroutine(launchRoutine);
            launchRoutine = null;
        }

        if (trailInstance != null)
        {
            trailInstance.SetActive(false);
        }

        RestoreBehaviours();
    }

    private IEnumerator LaunchRoutine()
    {
        CacheRocketState();
        DisableBehaviours();
        SetTerminalInput(false);
        if (launchOverlay != null)
        {
            launchOverlay.SetActive(true);
        }

        yield return PrintLaunchText("WARNING");
        yield return PrintLaunchText("REACTOR CORE FAILURE IMMINENT");
        yield return PrintLaunchText("NEXUS-01 LAUNCH SEQUENCE INITIATED");
        yield return new WaitForSecondsRealtime(0.35f);

        for (int countdown = 5; countdown >= 1; countdown--)
        {
            yield return PrintLaunchText(countdown.ToString());
            yield return new WaitForSecondsRealtime(countdownStepDuration);
        }

        yield return PrintLaunchText("LAUNCH");
        PrepareRocket();
        yield return ShakeRocket();
        yield return MoveRocketToExit();

        if (explosionEffect != null)
        {
            explosionEffect.PlayLaunchEffect(rocket);
        }

        RestoreBehaviours();
        launchRoutine = null;

        if (levelTransition != null)
        {
            levelTransition.LoadSpaceShooter();
        }
    }

    private IEnumerator PrintLaunchText(string text)
    {
        if (countdownText != null)
        {
            countdownText.text = text;
        }

        if (terminalUI != null)
        {
            terminalUI.AppendInstant(text + "\n");
        }

        yield return new WaitForSecondsRealtime(0.12f);
    }

    private void CacheRocketState()
    {
        if (rocket == null)
        {
            return;
        }

        rocketStartPosition = rocket.position;
        rocketStartRotation = rocket.rotation;
    }

    private void PrepareRocket()
    {
        if (rocket == null)
        {
            return;
        }

        if (launchStartMarker != null)
        {
            rocket.position = launchStartMarker.position;
            rocket.rotation = launchStartMarker.rotation;
        }

        if (rocketAnimator != null && flightController != null)
        {
            rocketAnimator.runtimeAnimatorController = flightController;
            rocketAnimator.enabled = true;
        }

        if (rocketTrailPrefab != null && trailInstance == null)
        {
            trailInstance = Instantiate(rocketTrailPrefab, rocket.position, rocket.rotation, rocket);
            trailInstance.transform.localPosition = Vector3.zero;
            trailInstance.transform.localRotation = Quaternion.identity;
        }

        if (trailInstance != null)
        {
            trailInstance.SetActive(true);
        }

        if (launchEffectPrefab != null)
        {
            GameObject effect = Instantiate(launchEffectPrefab, rocket.position, rocket.rotation);
            Destroy(effect, 5f);
        }
    }

    private IEnumerator ShakeRocket()
    {
        if (rocket == null)
        {
            yield break;
        }

        Vector3 basePosition = rocket.position;
        float elapsed = 0f;
        while (elapsed < preLaunchShakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            rocket.position = basePosition + Random.insideUnitSphere * 0.025f;
            yield return null;
        }

        rocket.position = basePosition;
    }

    private IEnumerator MoveRocketToExit()
    {
        if (rocket == null)
        {
            yield break;
        }

        Vector3 start = rocket.position;
        Vector3 exit = launchExitMarker != null
            ? launchExitMarker.position
            : start + Vector3.up * 25f;
        float elapsed = 0f;

        while (elapsed < launchDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / launchDuration);
            float easedTime = normalizedTime * normalizedTime;
            rocket.position = Vector3.Lerp(start, exit, easedTime);

            if (launchCamera != null)
            {
                Vector3 desiredPosition = rocket.position - rocket.forward * cameraFollowDistance + Vector3.up * cameraFollowHeight;
                launchCamera.transform.position = Vector3.Lerp(launchCamera.transform.position, desiredPosition, Time.unscaledDeltaTime * 4f);
                launchCamera.transform.LookAt(rocket.position);
            }

            yield return null;
        }

        rocket.position = exit;
    }

    private void SetTerminalInput(bool enabled)
    {
        if (terminalUI != null)
        {
            terminalUI.SetCommandInputEnabled(enabled);
        }
    }

    private void DisableBehaviours()
    {
        if (behavioursToDisable == null)
        {
            return;
        }

        behaviourStates = new bool[behavioursToDisable.Length];
        behaviourStatesCached = new bool[behavioursToDisable.Length];
        for (int i = 0; i < behavioursToDisable.Length; i++)
        {
            Behaviour behaviour = behavioursToDisable[i];
            if (behaviour == null)
            {
                continue;
            }

            behaviourStates[i] = behaviour.enabled;
            behaviourStatesCached[i] = true;
            behaviour.enabled = false;
        }
    }

    private void RestoreBehaviours()
    {
        if (behavioursToDisable != null && behaviourStates != null && behaviourStatesCached != null)
        {
            for (int i = 0; i < behavioursToDisable.Length; i++)
            {
                if (behavioursToDisable[i] != null && behaviourStatesCached[i])
                {
                    behavioursToDisable[i].enabled = behaviourStates[i];
                }
            }
        }

        if (levelFlow == null || levelFlow.CurrentState != LevelFlowController.LevelState.LAUNCHING)
        {
            SetTerminalInput(true);
        }
    }
}
