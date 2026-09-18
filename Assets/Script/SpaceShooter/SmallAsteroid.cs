using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class SmallAsteroid : MonoBehaviour
{
    private const float DefaultRotationSpeed = 90f;
    private const float DefaultLifetime = 9f;
    private const float DefaultSpeed = 3.5f;
    private const float DefaultHomingStrength = 5.5f;
    private const float MinimumVisibleViewport = -0.25f;
    private const float MaximumVisibleViewport = 1.25f;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float rotationSpeed = DefaultRotationSpeed;
    [SerializeField, Min(0.1f)] private float lifetime = DefaultLifetime;
    [SerializeField, Min(0.1f)] private float speed = DefaultSpeed;
    [SerializeField, Min(0f)] private float homingStrength = DefaultHomingStrength;

    private Rigidbody2D body;
    private Camera playCamera;
    private GameObject impactEffectPrefab;
    private float remainingLifetime;
    private Transform targetRocket;
    private bool resolved;
    private bool isGateBlocker;
    private bool gateNotified;
    private SpaceGameplayController gameplayController;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.useFullKinematicContacts = true;
        Collider2D fragmentCollider = GetComponent<Collider2D>();
        fragmentCollider.isTrigger = true;
    }

    private void Update()
    {
        if (resolved)
        {
            return;
        }

        transform.Rotate(Vector3.forward, rotationSpeed * Time.unscaledDeltaTime);
        if (!isGateBlocker && targetRocket != null)
        {
            Vector2 directionToRocket = ((Vector2)targetRocket.position - body.position).normalized;
            Vector2 desiredVelocity = directionToRocket * speed;
            float steering = 1f - Mathf.Exp(-homingStrength * Time.unscaledDeltaTime);
            body.linearVelocity = Vector2.Lerp(body.linearVelocity, desiredVelocity, steering);
        }

        remainingLifetime -= Time.unscaledDeltaTime;
        if (remainingLifetime <= 0f || IsOutsideCamera())
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Initializes a small asteroid with movement, visuals, and optional path-gate behavior.
    /// </summary>
    public void Initialize(Vector2 direction, float fragmentSpeed, Camera camera, GameObject visualPrefab, GameObject explosionPrefab, bool gateBlocker = false, SpaceGameplayController controller = null, Transform target = null)
    {
        playCamera = camera;
        targetRocket = target;
        speed = Mathf.Max(0.1f, fragmentSpeed);
        isGateBlocker = gateBlocker;
        gameplayController = controller;
        remainingLifetime = gateBlocker ? 60f : Mathf.Max(0.1f, lifetime);
        body.linearVelocity = direction.sqrMagnitude > 0.001f ? direction.normalized * speed : Vector2.zero;
        Collider2D asteroidCollider = GetComponent<Collider2D>();
        asteroidCollider.isTrigger = false;
        AttachVisual(visualPrefab);
        impactEffectPrefab = explosionPrefab;
    }

    /// <summary>
    /// Resolves a projectile hit with a small impact effect.
    /// </summary>
    public void DestroyByProjectile()
    {
        ResolveAndDestroy();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (resolved)
        {
            return;
        }

        RocketController2D rocket = other.GetComponentInParent<RocketController2D>();
        if (rocket == null)
        {
            return;
        }

        rocket.TakeFragmentDamage();
        ResolveAndDestroy();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (resolved)
        {
            return;
        }

        RocketController2D rocket = collision.collider.GetComponentInParent<RocketController2D>();
        if (rocket == null)
        {
            return;
        }

        rocket.TakeFragmentDamage();
        if (!isGateBlocker)
        {
            ResolveAndDestroy();
        }
    }

    private void AttachVisual(GameObject visualPrefab)
    {
        if (visualPrefab == null)
        {
            return;
        }

        GameObject visual = Instantiate(visualPrefab, transform);
        visual.name = "FragmentVisual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        float visualScale = Random.Range(0.28f, 0.5f);
        visual.transform.localScale = Vector3.one * visualScale;
    }

    private void ResolveAndDestroy()
    {
        if (resolved)
        {
            return;
        }

        resolved = true;
        if (isGateBlocker && !gateNotified)
        {
            gateNotified = true;
            if (gameplayController != null)
            {
                gameplayController.NotifyGateBlockerDestroyed();
            }
        }

        if (impactEffectPrefab != null)
        {
            GameObject effect = Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 4f);
        }

        Destroy(gameObject);
    }

    private bool IsOutsideCamera()
    {
        if (playCamera == null || !playCamera.orthographic)
        {
            return false;
        }

        Vector3 viewportPosition = playCamera.WorldToViewportPoint(transform.position);
        return viewportPosition.x < MinimumVisibleViewport || viewportPosition.x > MaximumVisibleViewport || viewportPosition.y < MinimumVisibleViewport || viewportPosition.y > MaximumVisibleViewport;
    }
}
