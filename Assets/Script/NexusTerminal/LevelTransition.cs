using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class LevelTransition : MonoBehaviour
{
    private const string DefaultSpaceScenePath = "Assets/Scenes/SpaceShooter.unity";
    private const string DefaultFailureMessage = "NEXUS-01 / ESCAPE ATTEMPT TERMINATED";
    private const string DefaultSuccessMessage = "NEXUS-01\n\nESCAPE TRAJECTORY CONFIRMED\n\nTHE ORBITAL ROUTE IS CLEAR\n\nLEVEL 4 CHIP ACQUIRED";
    private const string DefaultNextLevelButtonLabel = "NEXT LEVEL";

    [Header("Scene Names")]
    [SerializeField] private string spaceSceneNameOrPath = DefaultSpaceScenePath;
    [SerializeField] private string nextSceneNameOrPath;

    [Header("Result Overlay")]
    [SerializeField] private GameObject resultOverlay;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private string sequenceResultMessage = DefaultFailureMessage;
    [SerializeField] private string escapeSuccessMessage = DefaultSuccessMessage;
    [SerializeField] private string nextLevelButtonLabel = DefaultNextLevelButtonLabel;

    private bool transitionStarted;

    /// <summary>
    /// Loads the configured space-shooter scene once.
    /// </summary>
    public void LoadSpaceShooter()
    {
        if (transitionStarted || string.IsNullOrWhiteSpace(spaceSceneNameOrPath))
        {
            return;
        }

        transitionStarted = true;
        SceneManager.LoadScene(spaceSceneNameOrPath.Trim());
    }

    /// <summary>
    /// Displays the configured failure result for a controlled setup error.
    /// </summary>
    public void ShowSequenceResult()
    {
        ShowResult(sequenceResultMessage);
    }

    /// <summary>
    /// Displays the successful orbital escape ending.
    /// </summary>
    public void ShowEscapeSuccess()
    {
        ShowResult(escapeSuccessMessage);
    }

    /// <summary>
    /// Loads an optional next scene, or keeps the success overlay active when none is configured.
    /// </summary>
    public void LoadNextLevelOrFinish()
    {
        if (string.IsNullOrWhiteSpace(nextSceneNameOrPath))
        {
            ShowEscapeSuccess();
            return;
        }

        if (transitionStarted)
        {
            return;
        }

        transitionStarted = true;
        SceneManager.LoadScene(nextSceneNameOrPath.Trim());
    }

    private void ShowResult(string message)
    {
        if (resultOverlay != null)
        {
            resultOverlay.SetActive(true);
        }

        if (resultText != null)
        {
            resultText.text = message;
        }
    }
}
