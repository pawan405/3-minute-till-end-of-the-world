using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class RocketController2D : MonoBehaviour
{
    private const float DefaultMoveSpeed = 7f;
    private const float DefaultFireCooldown = 0.22f;
    private const float DefaultProjectileSpeed = 14f;
    private const float DefaultProjectileLifetime = 3f;
    private const int DefaultMaxHealth = 3;
    private const float DefaultForwardSpeed = 1.25f;
    private const float DefaultFlightAnimationSpeed = 8f;
    private const float DefaultFlightScalePulse = 0.025f;
    private const float ProjectileSpawnOffset = 0.8f;
    private const float ProjectileVisualScale = 0.12f;
    private const string ProjectileShaderName = "Universal Render Pipeline/Unlit";

    [Header("Dependencies")]
    [SerializeField] private LevelFlowController levelFlow;
    [SerializeField] private AsteroidSpawner asteroidSpawner;
    [SerializeField] private ExplosionEffect explosionEffect;
    [SerializeField] private SpaceGameplayController gameplayController;
    [SerializeField] private Camera playCamera;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float moveSpeed = DefaultMoveSpeed;
    [SerializeField, Min(0f)] private float screenPadding = 0.45f;
    [SerializeField, Min(0f)] private float forwardSpeed = DefaultForwardSpeed;

    [Header("Flight Animation")]
    [SerializeField, Min(0.1f)] private float flightAnimationSpeed = DefaultFlightAnimationSpeed;
    [SerializeField, Range(0f, 0.1f)] private float flightScalePulse = DefaultFlightScalePulse;

    [Header("Weapon")]
    [SerializeField, Min(0.05f)] private float fireCooldown = DefaultFireCooldown;
    [SerializeField, Min(0.1f)] private float projectileSpeed = DefaultProjectileSpeed;
    [SerializeField, Min(0.1f)] private float projectileLifetime = DefaultProjectileLifetime;
    [SerializeField] private bool automaticFire = true;

    [Header("Health")]
    [SerializeField, Min(1)] private int maxHealth = DefaultMaxHealth;

    private Rigidbody2D body;
    private bool controlEnabled;
    private Vector2 moveInput;
    private float nextFireTime;
    private int currentHealth;
    private Vector3 baseScale;
    private float flightAnimationTime;

    public Vector2 MoveInput => moveInput;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        currentHealth = Mathf.Max(1, maxHealth);
        baseScale = transform.localScale;
    }

    private void Update()
    {
        AnimateFlight();
        if (Keyboard.current == null && !automaticFire)
        {
            moveInput = Vector2.zero;
            return;
        }

        if (automaticFire && controlEnabled)
        {
            TryFireProjectile();
        }

        if (Keyboard.current == null)
        {
            moveInput = Vector2.zero;
            return;
        }

        if (Keyboard.current.spaceKey.isPressed)
        {
            TryFireProjectile();
        }

        if (!controlEnabled)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = new Vector2(
            (Keyboard.current.dKey.isPressed ? 1f : 0f) - (Keyboard.current.aKey.isPressed ? 1f : 0f),
            (Keyboard.current.wKey.isPressed ? 1f : 0f) - (Keyboard.current.sKey.isPressed ? 1f : 0f));
        moveInput = Vector2.ClampMagnitude(moveInput, 1f);
    }

    private void FixedUpdate()
    {
        if (!controlEnabled || body == null)
        {
            return;
        }

        body.linearVelocity = moveInput * moveSpeed + Vector2.up * forwardSpeed;
        ClampToCameraBounds();
    }

    private void AnimateFlight()
    {
        flightAnimationTime += Time.unscaledDeltaTime * flightAnimationSpeed;
        float pulse = 1f + Mathf.Sin(flightAnimationTime) * flightScalePulse;
        transform.localScale = baseScale * pulse;
    }

    /// <summary>
    /// Connects the controller to the active space-shooter flow and camera.
    /// </summary>
    public void Configure(LevelFlowController flow, AsteroidSpawner spawner, ExplosionEffect effect, Camera camera, SpaceGameplayController controller)
    {
        levelFlow = flow;
        asteroidSpawner = spawner;
        explosionEffect = effect;
        playCamera = camera;
        gameplayController = controller;
    }

    /// <summary>
    /// Disables movement and firing while preserving the rocket for a retry.
    /// </summary>
    public void DisableControl()
    {
        controlEnabled = false;
        moveInput = Vector2.zero;
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
    }

    /// <summary>
    /// Handles a legacy asteroid collision by routing it through fragment damage.
    /// </summary>
    public void HandleAsteroidCollision()
    {
        TakeFragmentDamage();
    }
    /// <summary>
    /// Applies one point of fragment damage to the rocket.
    /// </summary>
    public void TakeFragmentDamage()
    {
        if (!controlEnabled || currentHealth <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - 1);
        if (explosionEffect != null)
        {
            explosionEffect.PlayRocketImpact(transform.position);
        }

        if (gameplayController != null)
        {
            gameplayController.NotifyRocketDamaged(currentHealth, maxHealth);
        }

        if (currentHealth == 0)
        {
            DisableControl();
            if (gameplayController != null)
            {
                gameplayController.NotifyRocketDestroyed();
            }
        }
    }

    /// <summary>
    /// Resets health, movement, and firing state for a new space encounter attempt.
    /// </summary>
    public void ResetForEncounter()
    {
        currentHealth = Mathf.Max(1, maxHealth);
        nextFireTime = 0f;
        moveInput = Vector2.zero;
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
    }

    /// <summary>
    /// Enables player control at the beginning of the active asteroid encounter.
    /// </summary>
    public void EnableControl()
    {
        ResetForEncounter();
        controlEnabled = true;
    }

    private void TryFireProjectile()
    {
        if (Time.time < nextFireTime)
        {
            return;
        }

        nextFireTime = Time.time + fireCooldown;
        GameObject projectileObject = new GameObject("RocketProjectile", typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(RocketProjectile));
        projectileObject.transform.position = transform.position + Vector3.up * ProjectileSpawnOffset;
        projectileObject.transform.rotation = Quaternion.identity;

        Rigidbody2D projectileBody = projectileObject.GetComponent<Rigidbody2D>();
        projectileBody.bodyType = RigidbodyType2D.Kinematic;
        projectileBody.gravityScale = 0f;

        CircleCollider2D projectileCollider = projectileObject.GetComponent<CircleCollider2D>();
        projectileCollider.radius = 0.12f;
        projectileCollider.isTrigger = true;

        CreateProjectileVisual(projectileObject.transform);
        RocketProjectile projectile = projectileObject.GetComponent<RocketProjectile>();
        projectile.Initialize(Vector2.up, projectileSpeed, projectileLifetime);
    }

    private static void CreateProjectileVisual(Transform projectileTransform)
    {
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "ProjectileGlow";
        visual.transform.SetParent(projectileTransform, false);
        visual.transform.localScale = Vector3.one * ProjectileVisualScale;

        Collider visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null)
        {
            Destroy(visualCollider);
        }

        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        Shader shader = Shader.Find(ProjectileShaderName);
        if (shader != null)
        {
            Material material = new Material(shader);
            material.color = new Color(0.2f, 0.85f, 1f, 1f);
            renderer.sharedMaterial = material;
        }
    }

    private void ClampToCameraBounds()
    {
        if (playCamera == null || !playCamera.orthographic)
        {
            return;
        }

        float verticalExtent = playCamera.orthographicSize;
        float horizontalExtent = verticalExtent * playCamera.aspect;
        Vector3 position = transform.position;
        position.x = Mathf.Clamp(position.x, playCamera.transform.position.x - horizontalExtent + screenPadding, playCamera.transform.position.x + horizontalExtent - screenPadding);
        position.y = Mathf.Clamp(position.y, playCamera.transform.position.y - verticalExtent + screenPadding, playCamera.transform.position.y + verticalExtent - screenPadding);
        transform.position = position;
    }
}
