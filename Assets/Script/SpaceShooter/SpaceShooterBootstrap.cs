using UnityEngine;

[DisallowMultipleComponent]
public sealed class SpaceShooterBootstrap : MonoBehaviour
{
    [Header("Progression")]
    [SerializeField] private LevelFlowController levelFlow;
    [SerializeField] private LevelTransition levelTransition;

    [Header("Gameplay")]
    [SerializeField] private RocketController2D rocket;
    [SerializeField] private AsteroidSpawner asteroidSpawner;
    [SerializeField] private ExplosionEffect explosionEffect;
    [SerializeField] private SpaceGameplayController gameplayController;
    [SerializeField] private Camera playCamera;

    private const float GameplayTimeScale = 1f;

    private void Start()
    {
        Time.timeScale = GameplayTimeScale;
        if (levelFlow == null || gameplayController == null)
        {
            ShowControlledFailure("SPACE SYSTEM OFFLINE\nGAMEPLAY FLOW NOT CONFIGURED");
            return;
        }

        levelFlow.EnterSpaceShooter();
        if (rocket == null || asteroidSpawner == null || playCamera == null)
        {
            ShowControlledFailure("SPACE SYSTEM OFFLINE\nGAMEPLAY REFERENCES INCOMPLETE");
            return;
        }

        gameplayController.Configure(levelFlow, levelTransition, rocket, asteroidSpawner, explosionEffect, playCamera);
        rocket.Configure(levelFlow, asteroidSpawner, explosionEffect, playCamera, gameplayController);
        asteroidSpawner.Configure(playCamera, rocket.transform);
        gameplayController.BeginEncounter();
    }

    private void ShowControlledFailure(string message)
    {
        if (levelTransition != null)
        {
            levelTransition.ShowSequenceResult();
        }

        if (levelFlow != null)
        {
            levelFlow.EnterSpaceShooter();
        }

        Debug.LogWarning(message, this);
    }
}
