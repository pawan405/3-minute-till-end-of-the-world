using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class BigAsteroid : MonoBehaviour
{
    private const int DefaultMaxHealth = 5;
    private const int DefaultFragmentCountPerHit = 3;
    private const float DefaultFragmentSpeed = 3.8f;
    private const float DefaultShakeDuration = 0.32f;
    private const float DefaultShakeMagnitude = 0.18f;
    private const float FinalImpactDelay = 0.28f;

    [Header("Health")]
    [SerializeField, Min(2)] private int maxHealth = DefaultMaxHealth;
    [SerializeField, Min(1)] private int fragmentCountPerHit = DefaultFragmentCountPerHit;

    [Header("Presentation")]
    [SerializeField, Min(0.1f)] private float fragmentSpeed = DefaultFragmentSpeed;
    [SerializeField, Min(0f)] private float shakeDuration = DefaultShakeDuration;
    [SerializeField, Min(0f)] private float shakeMagnitude = DefaultShakeMagnitude;
    [SerializeField] private GameObject fragmentVisualPrefab;
    [SerializeField] private GameObject smallExplosionPrefab;
    [SerializeField] private GameObject finalExplosionPrefab;

    private Rigidbody2D body;
    private Collider2D asteroidCollider;
    private SpaceGameplayController gameplayController;
    private Camera playCamera;
    private Transform targetRocket;
    private Vector3 basePosition;
    private Vector3 baseScale;
    private int currentHealth;
    private float nextDamageTime;
    private bool initialized;
    private bool destroyed;
    private bool vulnerable;
    private bool spawnFragments = true;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        asteroidCollider = GetComponent<Collider2D>();
        asteroidCollider.isTrigger = true;
        baseScale = transform.localScale;
    }

    /// <summary>
    /// Connects the boss asteroid to the space encounter and initializes its health.
    /// </summary>
    public void Initialize(SpaceGameplayController controller, Camera camera, GameObject fragmentVisual, GameObject smallExplosion, GameObject finalExplosion, Transform rocketTransform = null, bool createFragments = true)
    {
        gameplayController = controller;
        playCamera = camera;
        targetRocket = rocketTransform;
        spawnFragments = createFragments;
        fragmentVisualPrefab = fragmentVisual;
        smallExplosionPrefab = smallExplosion;
        finalExplosionPrefab = finalExplosion;
        currentHealth = Mathf.Max(2, maxHealth);
        basePosition = transform.position;
        baseScale = transform.localScale;
        initialized = true;
        destroyed = false;
        vulnerable = true;
        nextDamageTime = 0f;
    }

    /// <summary>
    /// Opens or closes the boss core after the asteroid gate is cleared.
    /// </summary>
    public void SetVulnerable(bool value)
    {
        vulnerable = value;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (destroyed)
        {
            return;
        }

        RocketController2D rocket = collision.collider.GetComponentInParent<RocketController2D>();
        if (rocket != null)
        {
            rocket.TakeFragmentDamage();
        }
    }

    /// <summary>
    /// Applies one projectile hit to the asteroid and starts its next damage stage.
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (!initialized || destroyed || Time.time < nextDamageTime)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - Mathf.Max(1, damage));
        nextDamageTime = Time.time + 0.2f;
        if (spawnFragments)
        {
            SpawnFragments();
        }

        if (gameplayController != null)
        {
            gameplayController.NotifyBigAsteroidDamaged(currentHealth, maxHealth);
        }

        if (currentHealth <= 0)
        {
            StartCoroutine(FinalDestructionRoutine());
            return;
        }

        ApplyDamageVisuals();
        StartCoroutine(ShakeRoutine());
    }

    private void ApplyDamageVisuals()
    {
        float damageRatio = 1f - (currentHealth / (float)Mathf.Max(1, maxHealth));
        float damageScale = 1f + damageRatio * 0.08f;
        transform.localScale = baseScale * damageScale;

        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Material material = renderers[i].material;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.Lerp(new Color(0.36f, 0.38f, 0.42f, 1f), new Color(0.16f, 0.17f, 0.2f, 1f), damageRatio));
            }
        }
    }

    private void SpawnFragments()
    {
        if (fragmentVisualPrefab == null)
        {
            return;
        }

        for (int i = 0; i < fragmentCountPerHit; i++)
        {
            GameObject fragmentObject = new GameObject("SmallAsteroid", typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SmallAsteroid));
            fragmentObject.transform.position = transform.position + (Vector3)(Random.insideUnitCircle * 1.1f);
            fragmentObject.transform.localScale = Vector3.one * Random.Range(0.7f, 1.1f);

            CircleCollider2D fragmentCollider = fragmentObject.GetComponent<CircleCollider2D>();
            fragmentCollider.radius = 0.34f;
            fragmentCollider.isTrigger = true;

            SmallAsteroid fragment = fragmentObject.GetComponent<SmallAsteroid>();
            Vector2 direction = targetRocket != null
                ? ((Vector2)targetRocket.position - (Vector2)fragmentObject.transform.position + Random.insideUnitCircle * 0.2f).normalized
                : (Random.insideUnitCircle + Vector2.left * 0.35f).normalized;
            fragment.Initialize(direction, fragmentSpeed + Random.Range(-0.6f, 0.8f), playCamera, fragmentVisualPrefab, smallExplosionPrefab, false, gameplayController, targetRocket);
        }
    }

    private IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;
        while (elapsed < shakeDuration && !destroyed)
        {
            elapsed += Time.unscaledDeltaTime;
            transform.position = basePosition + (Vector3)(Random.insideUnitCircle * shakeMagnitude);
            yield return null;
        }

        if (!destroyed)
        {
            transform.position = basePosition;
        }
    }

    private IEnumerator FinalDestructionRoutine()
    {
        destroyed = true;
        asteroidCollider.enabled = false;
        body.linearVelocity = Vector2.zero;

        if (gameplayController != null)
        {
            gameplayController.NotifyBigAsteroidCritical();
        }

        yield return new WaitForSecondsRealtime(FinalImpactDelay);
        if (finalExplosionPrefab != null)
        {
            GameObject effect = Instantiate(finalExplosionPrefab, transform.position, Quaternion.identity);
            effect.transform.localScale = Vector3.one * 2.5f;
            Destroy(effect, 6f);
        }

        if (gameplayController != null)
        {
            gameplayController.NotifyBigAsteroidDestroyed(transform.position);
        }

        Destroy(gameObject);
    }
}
