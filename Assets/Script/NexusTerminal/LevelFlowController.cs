using UnityEngine;

[DisallowMultipleComponent]
public sealed class LevelFlowController : MonoBehaviour
{
    public enum LevelState
    {
        TERMINAL,
        REACTOR_ACCESS,
        LAUNCH_READY,
        LAUNCHING,
        SPACE_SHOOTER,
        COMPLETE
    }

    [Header("Progression References")]
    [SerializeField] private ReactorAccess reactorAccess;
    [SerializeField] private RocketLaunchSequence rocketLaunchSequence;
    [SerializeField] private LevelTransition levelTransition;

    [Header("Initial State")]
    [SerializeField] private LevelState initialState = LevelState.TERMINAL;

    private LevelState currentState;
    private bool reactorFileAccessed;
    private bool reactorAuthorizationAccepted;

    public LevelState CurrentState => currentState;
    public bool ReactorFileAccessed => reactorFileAccessed;
    public bool ReactorAuthorizationAccepted => reactorAuthorizationAccepted;

    private void Awake()
    {
        currentState = initialState;
    }

    /// <summary>
    /// Routes a complete terminal line through the authoritative progression state machine.
    /// </summary>
    public bool TryHandleTerminalCommand(string rawInput, out string response)
    {
        string command = rawInput == null ? string.Empty : rawInput.Trim();
        response = string.Empty;

        if (string.IsNullOrWhiteSpace(command))
        {
            return false;
        }

        if (currentState == LevelState.TERMINAL)
        {
            if (string.Equals(command, "reactor", System.StringComparison.OrdinalIgnoreCase))
            {
                if (reactorAccess != null && reactorAccess.TryBeginAccess(command, out response))
                {
                    return true;
                }
            }

            if (string.Equals(command, "reactor access", System.StringComparison.OrdinalIgnoreCase))
            {
                if (reactorAccess == null)
                {
                    response = "REACTOR ACCESS OFFLINE\nCONTACT NEXUS SYSTEMS ADMINISTRATION.";
                    return true;
                }

                bool handled = reactorAccess.TryBeginAccess(command, out response);
                if (handled)
                {
                    currentState = LevelState.REACTOR_ACCESS;
                }

                return handled;
            }

            if (string.Equals(command, "launch", System.StringComparison.OrdinalIgnoreCase))
            {
                response = "LAUNCH DENIED\nREACTOR AUTHORIZATION REQUIRED";
                return true;
            }

            response = string.Empty;
            return false;
        }

        if (currentState == LevelState.REACTOR_ACCESS)
        {
            if (reactorAccess != null && reactorAccess.TrySubmitAccessCode(command, out response))
            {
                return true;
            }

            response = "ACCESS PANEL ACTIVE\nENTER THE REACTOR ACCESS CODE.";
            return true;
        }

        if (string.Equals(command, "launch", System.StringComparison.OrdinalIgnoreCase))
        {
            if (currentState != LevelState.LAUNCH_READY || !reactorAuthorizationAccepted)
            {
                response = "LAUNCH DENIED\nREACTOR AUTHORIZATION REQUIRED";
                return true;
            }

            BeginLaunch();
            response = "EMERGENCY LAUNCH PROTOCOL ACTIVATED\nREACTOR CORE FAILURE IMMINENT";
            return true;
        }

        if (currentState == LevelState.LAUNCHING)
        {
            response = "LAUNCH SEQUENCE IN PROGRESS";
            return true;
        }

        if (currentState == LevelState.SPACE_SHOOTER)
        {
            response = "SPACE CONTROL ACTIVE\nUSE W A S D TO PILOT NEXUS-01\nSPACE TO FIRE";
            return true;
        }

        if (currentState == LevelState.COMPLETE)
        {
            response = "ESCAPE TRAJECTORY CONFIRMED";
            return true;
        }

        return false;
    }

    /// <summary>
    /// Records successful reactor file authorization and advances to launch readiness.
    /// </summary>
    public void SetReactorAuthorizationAccepted()
    {
        reactorFileAccessed = true;
        reactorAuthorizationAccepted = true;

        if (currentState == LevelState.REACTOR_ACCESS)
        {
            currentState = LevelState.LAUNCH_READY;
        }
    }

    /// <summary>
    /// Starts the launch sequence directly from the nearby launch interaction.
    /// </summary>
    public void BeginAutomaticLaunch()
    {
        if (currentState == LevelState.LAUNCHING || currentState == LevelState.SPACE_SHOOTER || currentState == LevelState.COMPLETE)
        {
            return;
        }

        reactorFileAccessed = true;
        reactorAuthorizationAccepted = true;
        currentState = LevelState.LAUNCH_READY;
        BeginLaunch();
    }

    /// <summary>
    /// Starts the launch presentation after launch authorization is satisfied.
    /// </summary>
    public void BeginLaunch()
    {
        if (currentState != LevelState.LAUNCH_READY || !reactorAuthorizationAccepted)
        {
            return;
        }

        currentState = LevelState.LAUNCHING;
        if (rocketLaunchSequence != null)
        {
            rocketLaunchSequence.PlayLaunch();
        }
    }

    /// <summary>
    /// Marks entry into the separate space-shooter scene.
    /// </summary>
    public void EnterSpaceShooter()
    {
        if (currentState == LevelState.COMPLETE)
        {
            return;
        }

        currentState = LevelState.SPACE_SHOOTER;
    }

    /// <summary>
    /// Completes the sequence after the player clears the orbital route.
    /// </summary>
    public void CompleteSequence()
    {
        if (currentState != LevelState.SPACE_SHOOTER)
        {
            return;
        }

        currentState = LevelState.COMPLETE;
        ChipProgress.UnlockLevel4Chip();
        if (levelTransition != null)
        {
            levelTransition.ShowEscapeSuccess();
        }
    }
}
