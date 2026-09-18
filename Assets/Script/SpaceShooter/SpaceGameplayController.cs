using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class SpaceGameplayController : MonoBehaviour
{
    private const string BigAsteroidName = "BigAsteroid";
    private const string NavigationCalculatingMessage = "NEXUS-01 NAVIGATION\n\nORBITAL TRAJECTORY CALCULATING...";
    private const string TemporaryHopeMessage = "ORBITAL TRAJECTORY: CLEAR\n\nESCAPE VECTOR LOCKED";
    private const string TrajectoryBlockedMessage = "WARNING\n\nTRAJECTORY BLOCKED\nOBJECT DETECTED";
    private const string CriticalObjectMessage = "OBJECT SIZE: CRITICAL\n\nCOLLISION COURSE: CONFIRMED";
    private const string CombatInstructionMessage = "ORBITAL ESCAPE ROUTE BLOCKED\n\nDESTROY THE ASTEROID\nWASD: PILOT    SPACE: FIRE";
    private const string RocketDestroyedMessage = "ROCKET DESTROYED\n\nPRESS R TO RETRY THE SPACE ENCOUNTER";
    private const string PathClearMessage = "PATH CLEAR\n\nORBITAL TRAJECTORY RESTORED\nNEXUS-01 ESCAPE ROUTE CONFIRMED";
    private const float DefaultIntroDuration = 2.4f;
    private const float DefaultHopeDuration = 1.8f;
    private const float DefaultWarningDuration = 1.5f;
    private const float DefaultBossApproachDuration = 1.5f;
    private const float DefaultEscapeDuration = 4f;
    private const float BossStartX = 0f;
    private const float BossCombatX = 0f;
    private const float BossCombatY = 2.6f;
    private const float BossVisualScale = 2.2f;
    private const float ShieldOrbitRadius = 2.2f;
    private const float BossColliderRadius = 0.85f;
    private const int DefaultRocketHealth = 3;
    private const int DefaultBossHealth = 5;
    private const int GateBlockerCount = 5;
    private const string GateBlockedMessage = "ASTEROID SHIELD ACTIVE\n\nFIRE THROUGH THE GAPS\nWASD: PILOT    SPACE: FIRE";
    private const string GateProgressMessage = "SHIELD ASTEROIDS REMAINING: ";
    private const string BossExposedMessage = "CORE EXPOSED\n\nKEEP FIRING THROUGH THE GAPS\nSPACE: FIRE";
    private const int DefaultStarCount = 55;
    private const float DefaultPlanetScale = 1.45f;
    private const string SpaceBackdropName = "SpaceBackdrop";
    private const string UnlitShaderName = "Universal Render Pipeline/Unlit";

    private enum EncounterState
    {
        Intro,
        Combat,
        Destroyed,
        Victory
    }

    [Header("Progression")]
    [SerializeField] private LevelFlowController levelFlow;
    [SerializeField] private LevelTransition levelTransition;

    [Header("Gameplay")]
    [SerializeField] private RocketController2D rocket;
    [SerializeField] private AsteroidSpawner asteroidSpawner;
    [SerializeField] private ExplosionEffect explosionEffect;
    [SerializeField] private Camera playCamera;

    [Header("Encounter Assets")]
    [SerializeField] private GameObject bigAsteroidVisualPrefab;
    [SerializeField] private GameObject smallExplosionPrefab;
    [SerializeField] private GameObject finalExplosionPrefab;

    [Header("Encounter Timing")]
    [SerializeField, Min(0.1f)] private float introDuration = DefaultIntroDuration;
    [SerializeField, Min(0.1f)] private float hopeDuration = DefaultHopeDuration;
    [SerializeField, Min(0.1f)] private float warningDuration = DefaultWarningDuration;
    [SerializeField, Min(0.1f)] private float bossApproachDuration = DefaultBossApproachDuration;
    [SerializeField, Min(0.1f)] private float escapeDuration = DefaultEscapeDuration;

    [Header("Space Presentation")]
    [SerializeField, Min(0)] private int starCount = DefaultStarCount;
    [SerializeField, Min(0.1f)] private float planetScale = DefaultPlanetScale;

    [Header("HUD")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text healthText;

    private BigAsteroid bigAsteroid;
    private EncounterState encounterState;
    private Coroutine encounterRoutine;
    private Vector3 rocketStartPosition;
    private int rocketHealth = DefaultRocketHealth;
    private int remainingGateBlockers;

    /// <summary>
    /// Connects the controller to the active space scene systems.
    /// </summary>
    public void Configure(LevelFlowController flow, LevelTransition transition, RocketController2D rocketController, AsteroidSpawner spawner, ExplosionEffect effects, Camera camera)
    {
        levelFlow = flow;
        levelTransition = transition;
        rocket = rocketController;
        asteroidSpawner = spawner;
        explosionEffect = effects;
        playCamera = camera;
    }

    /// <summary>
    /// Starts the narrative navigation sequence before combat begins.
    /// </summary>
    public void BeginEncounter()
    {
        if (encounterRoutine != null)
        {
            return;
        }

        if (rocket != null)
        {
            rocketStartPosition = rocket.transform.position;
            rocket.ResetForEncounter();
            rocket.DisableControl();
        }

        if (asteroidSpawner != null)
        {
            asteroidSpawner.StopSpawning();
        }

        encounterRoutine = StartCoroutine(EncounterIntroRoutine());
    }

    /// <summary>
    /// Registers a cleared small asteroid from the boss approach gate.
    /// </summary>
    public void NotifyGateBlockerDestroyed()
    {
        if (remainingGateBlockers <= 0)
        {
            return;
        }

        remainingGateBlockers--;
        if (remainingGateBlockers == 0)
        {
            if (bigAsteroid != null)
            {
                bigAsteroid.SetVulnerable(true);
            }

            SetStatus(BossExposedMessage);
            UpdateHealthText("BIG ASTEROID", bigAsteroid != null ? bigAsteroid.CurrentHealth : DefaultBossHealth, bigAsteroid != null ? bigAsteroid.MaxHealth : DefaultBossHealth);
            return;
        }

        SetStatus(GateProgressMessage + remainingGateBlockers);
    }

    /// <summary>
    /// Receives a boss damage update and refreshes the combat HUD.
    /// </summary>
    public void NotifyBigAsteroidDamaged(int currentHealth, int maxHealth)
    {
        int damagePercent = Mathf.RoundToInt((1f - currentHealth / (float)Mathf.Max(1, maxHealth)) * 100f);
        SetStatus("ASTEROID STRUCTURAL DAMAGE: " + damagePercent + "%");
        UpdateHealthText("BIG ASTEROID", currentHealth, maxHealth);
    }

    /// <summary>
    /// Displays the final critical warning before the boss explosion.
    /// </summary>
    public void NotifyBigAsteroidCritical()
    {
        SetStatus("ASTEROID: CRITICAL\n\nSTRUCTURAL FAILURE IMMINENT");
    }

    /// <summary>
    /// Completes the encounter after the asteroid has been destroyed.
    /// </summary>
    public void NotifyBigAsteroidDestroyed(Vector3 position)
    {
        if (encounterState == EncounterState.Victory)
        {
            return;
        }

        encounterState = EncounterState.Victory;
        if (asteroidSpawner != null)
        {
            asteroidSpawner.StopSpawning();
        }

        if (encounterRoutine != null)
        {
            StopCoroutine(encounterRoutine);
        }

        encounterRoutine = StartCoroutine(EscapeRoutine());
    }

    /// <summary>
    /// Applies fragment damage to the rocket without ending the encounter immediately.
    /// </summary>
    public void NotifyRocketDamaged(int currentHealth, int maxHealth)
    {
        rocketHealth = currentHealth;
        UpdateHealthText("ROCKET HP", currentHealth, maxHealth);
        SetStatus("IMPACT DETECTED\nROCKET SYSTEMS DAMAGED");
    }

    /// <summary>
    /// Ends the current attempt and waits for a space-only retry.
    /// </summary>
    public void NotifyRocketDestroyed()
    {
        if (encounterState != EncounterState.Combat)
        {
            return;
        }

        encounterState = EncounterState.Destroyed;
        if (asteroidSpawner != null)
        {
            asteroidSpawner.StopSpawning();
        }

        SetStatus(RocketDestroyedMessage);
        UpdateHealthText("ROCKET HP", 0, DefaultRocketHealth);
    }

    private void Update()
    {
        if (encounterState != EncounterState.Destroyed || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartEncounter();
        }
    }

    private IEnumerator EncounterIntroRoutine()
    {
        encounterState = EncounterState.Intro;
        rocketHealth = DefaultRocketHealth;
        UpdateHealthText("ROCKET HP", rocketHealth, DefaultRocketHealth);
        SetStatus(NavigationCalculatingMessage);
        CreateBigAsteroid();

        yield return new WaitForSecondsRealtime(introDuration);
        SetStatus(TemporaryHopeMessage);
        yield return new WaitForSecondsRealtime(hopeDuration);
        SetStatus(TrajectoryBlockedMessage);
        yield return new WaitForSecondsRealtime(warningDuration);
        SetStatus(CriticalObjectMessage);
        yield return new WaitForSecondsRealtime(warningDuration);
        yield return StartCoroutine(ApproachBossRoutine());
        CreateGateBlockers();

        SetStatus(GateBlockedMessage);
        UpdateHealthText("BIG ASTEROID + SHIELD", DefaultBossHealth, DefaultBossHealth);
        encounterState = EncounterState.Combat;
        if (rocket != null)
        {
            rocket.ResetForEncounter();
            rocket.EnableControl();
        }

        if (asteroidSpawner != null)
        {
            asteroidSpawner.BeginSpawning();
        }

        encounterRoutine = null;
    }

    private IEnumerator ApproachBossRoutine()
    {
        if (bigAsteroid == null)
        {
            yield break;
        }

        Vector3 start = bigAsteroid.transform.position;
        Vector3 target = new Vector3(BossCombatX, BossCombatY, 0f);
        float elapsed = 0f;
        while (elapsed < bossApproachDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / bossApproachDuration);
            bigAsteroid.transform.position = Vector3.Lerp(start, target, normalizedTime);
            yield return null;
        }

        bigAsteroid.transform.position = target;
    }

    private void CreateGateBlockers()
    {
        remainingGateBlockers = GateBlockerCount;
        for (int i = 0; i < GateBlockerCount; i++)
        {
            float angle = (90f - i * (360f / GateBlockerCount)) * Mathf.Deg2Rad;
            Vector3 blockerPosition = new Vector3(
                BossCombatX + Mathf.Cos(angle) * ShieldOrbitRadius,
                BossCombatY + Mathf.Sin(angle) * ShieldOrbitRadius,
                0f);

            GameObject blockerObject = new GameObject("GateBlocker", typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SmallAsteroid));
            blockerObject.transform.position = blockerPosition;
            blockerObject.transform.localScale = Vector3.one * Random.Range(0.82f, 1.08f);

            CircleCollider2D blockerCollider = blockerObject.GetComponent<CircleCollider2D>();
            blockerCollider.radius = 0.42f;
            blockerCollider.isTrigger = false;

            SmallAsteroid blocker = blockerObject.GetComponent<SmallAsteroid>();
            blocker.Initialize(Vector2.zero, 0.1f, playCamera, bigAsteroidVisualPrefab, smallExplosionPrefab, true, this);
        }
    }

    private void CreateBigAsteroid()
    {
        CleanupAsteroidObjects();
        GameObject bossObject = new GameObject(BigAsteroidName, typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(BigAsteroid));
        bossObject.transform.position = new Vector3(BossStartX, BossCombatY, 0f);
        bossObject.transform.localScale = Vector3.one * BossVisualScale;

        Rigidbody2D bossBody = bossObject.GetComponent<Rigidbody2D>();
        bossBody.bodyType = RigidbodyType2D.Kinematic;
        bossBody.gravityScale = 0f;
        bossBody.useFullKinematicContacts = true;

        CircleCollider2D bossCollider = bossObject.GetComponent<CircleCollider2D>();
        bossCollider.radius = BossColliderRadius;
        bossCollider.isTrigger = false;

        if (bigAsteroidVisualPrefab != null)
        {
            GameObject visual = Instantiate(bigAsteroidVisualPrefab, bossObject.transform);
            visual.name = "BigAsteroidVisual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
        }

        bigAsteroid = bossObject.GetComponent<BigAsteroid>();
        bigAsteroid.Initialize(this, playCamera, bigAsteroidVisualPrefab, smallExplosionPrefab, finalExplosionPrefab, rocket != null ? rocket.transform : null);
        UpdateHealthText("BIG ASTEROID", bigAsteroid.CurrentHealth, bigAsteroid.MaxHealth);
    }

    private void RestartEncounter()
    {
        if (encounterRoutine != null)
        {
            StopCoroutine(encounterRoutine);
        }

        CleanupAsteroidObjects();
        if (asteroidSpawner != null)
        {
            asteroidSpawner.StopSpawning();
        }

        if (rocket != null)
        {
            rocket.gameObject.SetActive(true);
            rocket.transform.position = rocketStartPosition;
            rocket.ResetForEncounter();
            rocket.DisableControl();
        }

        encounterRoutine = StartCoroutine(EncounterIntroRoutine());
    }

    private void CleanupAsteroidObjects()
    {
        remainingGateBlockers = 0;
        BigAsteroid[] bosses = FindObjectsByType<BigAsteroid>(FindObjectsSortMode.None);
        for (int i = 0; i < bosses.Length; i++)
        {
            Destroy(bosses[i].gameObject);
        }

        SmallAsteroid[] fragments = FindObjectsByType<SmallAsteroid>(FindObjectsSortMode.None);
        for (int i = 0; i < fragments.Length; i++)
        {
            Destroy(fragments[i].gameObject);
        }

        Asteroid[] regularAsteroids = FindObjectsByType<Asteroid>(FindObjectsSortMode.None);
        for (int i = 0; i < regularAsteroids.Length; i++)
        {
            Destroy(regularAsteroids[i].gameObject);
        }
    }

    private IEnumerator EscapeRoutine()
    {
        if (rocket != null)
        {
            rocket.DisableControl();
        }

        SetStatus(PathClearMessage);
        UpdateHealthText("NEXUS-01", 1, 1);
        yield return new WaitForSecondsRealtime(escapeDuration);

        if (levelFlow != null)
        {
            levelFlow.CompleteSequence();
        }
        else if (levelTransition != null)
        {
            levelTransition.ShowEscapeSuccess();
        }

        encounterRoutine = null;
    }

    private void CreateSpaceBackdrop()
    {
        if (GameObject.Find(SpaceBackdropName) != null)
        {
            return;
        }

        GameObject backdrop = new GameObject(SpaceBackdropName);
        Material starMaterial = CreateUnlitMaterial(new Color(0.55f, 0.8f, 1f, 1f));
        for (int i = 0; i < starCount; i++)
        {
            GameObject star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            star.name = "Star";
            star.transform.SetParent(backdrop.transform, false);
            star.transform.position = new Vector3(Random.Range(-9f, 9f), Random.Range(-5f, 5f), 8f);
            float starScale = Random.Range(0.015f, 0.045f);
            star.transform.localScale = Vector3.one * starScale;
            RemovePrimitiveCollider(star);
            Renderer starRenderer = star.GetComponent<Renderer>();
            if (starRenderer != null)
            {
                starRenderer.sharedMaterial = starMaterial;
            }
        }

        GameObject planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        planet.name = "DistantWorld";
        planet.transform.SetParent(backdrop.transform, false);
        planet.transform.position = new Vector3(-7f, 3.2f, 7f);
        planet.transform.localScale = Vector3.one * planetScale;
        RemovePrimitiveCollider(planet);
        Renderer planetRenderer = planet.GetComponent<Renderer>();
        if (planetRenderer != null)
        {
            planetRenderer.sharedMaterial = CreateUnlitMaterial(new Color(0.04f, 0.22f, 0.52f, 1f));
        }
    }

    private static Material CreateUnlitMaterial(Color color)
    {
        Shader shader = Shader.Find(UnlitShaderName);
        if (shader == null)
        {
            return null;
        }

        Material material = new Material(shader);
        material.color = color;
        return material;
    }

    private static void RemovePrimitiveCollider(GameObject primitive)
    {
        Collider collider = primitive.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void UpdateHealthText(string label, int current, int maximum)
    {
        if (healthText != null)
        {
            healthText.text = label + ": " + current + "/" + maximum;
        }
    }
}
