using UnityEngine;

[DisallowMultipleComponent]
public sealed class Asteroid : MonoBehaviour
{
    [SerializeField, Min(0f)] private float rotationSpeed = 35f;
    [SerializeField, Min(0f)] private float despawnPadding = 3f;

    private Rigidbody2D body;
    private Camera playCamera;
    private bool impactProcessed;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        ConfigurePhysicsIfAvailable();
    }

    private void Update()
    {
        transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
        if (playCamera == null)
        {
            return;
        }

        Vector3 viewportPosition = playCamera.WorldToViewportPoint(transform.position);
        if (viewportPosition.x < -despawnPadding || viewportPosition.x > 1f + despawnPadding || viewportPosition.y < -despawnPadding || viewportPosition.y > 1f + despawnPadding)
        {
            Despawn();
        }
    }

    /// <summary>
    /// Initializes movement and camera-bound despawning for this asteroid.
    /// </summary>
    public void Initialize(Vector2 direction, float speed)
    {
        EnsurePhysicsComponents();
        body.linearVelocity = direction.normalized * speed;
    }

    /// <summary>
    /// Forwards the first impact to the rocket controller.
    /// </summary>
    public void OnRocketImpact()
    {
        if (impactProcessed)
        {
            return;
        }

        impactProcessed = true;
        RocketController2D rocket = FindFirstObjectByType<RocketController2D>();
        if (rocket != null)
        {
            rocket.HandleAsteroidCollision();
        }

        Despawn();
    }

    /// <summary>
    /// Removes this asteroid after a projectile hit.
    /// </summary>
    public void DestroyByProjectile()
    {
        Despawn();
    }

    /// <summary>
    /// Removes this asteroid from the active field.
    /// </summary>
    public void Despawn()
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// Assigns the camera used for viewport-bound cleanup.
    /// </summary>
    public void SetCamera(Camera camera)
    {
        playCamera = camera;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.GetComponentInParent<RocketController2D>() != null)
        {
            OnRocketImpact();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<RocketController2D>() != null)
        {
            OnRocketImpact();
        }
    }

    private void EnsurePhysicsComponents()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        if (body == null)
        {
            return;
        }

        ConfigurePhysicsIfAvailable();
        if (GetComponent<Collider2D>() == null)
        {
            CircleCollider2D collider = gameObject.AddComponent<CircleCollider2D>();
            collider.radius = 1f;
        }
    }

    private void ConfigurePhysicsIfAvailable()
    {
        if (body == null)
        {
            return;
        }

        body.gravityScale = 0f;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.useFullKinematicContacts = true;
    }
}
