using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class RocketProjectile : MonoBehaviour
{
    private const float DefaultLifetime = 3f;

    private Rigidbody2D body;
    private float remainingLifetime = DefaultLifetime;
    private bool resolved;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        Collider2D projectileCollider = GetComponent<Collider2D>();
        projectileCollider.isTrigger = true;
    }

    private void Update()
    {
        if (resolved)
        {
            return;
        }

        remainingLifetime -= Time.deltaTime;
        if (remainingLifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Launches the projectile in the rocket's forward screen direction.
    /// </summary>
    public void Initialize(Vector2 direction, float speed, float lifetime)
    {
        remainingLifetime = Mathf.Max(0.1f, lifetime);
        body.linearVelocity = direction.normalized * Mathf.Max(0.1f, speed);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (resolved)
        {
            return;
        }

        BigAsteroid bigAsteroid = other.GetComponentInParent<BigAsteroid>();
        if (bigAsteroid != null)
        {
            resolved = true;
            bigAsteroid.TakeDamage(1);
            Destroy(gameObject);
            return;
        }

        Asteroid asteroid = other.GetComponentInParent<Asteroid>();
        if (asteroid != null)
        {
            resolved = true;
            asteroid.DestroyByProjectile();
            Destroy(gameObject);
            return;
        }

        SmallAsteroid smallAsteroid = other.GetComponentInParent<SmallAsteroid>();
        if (smallAsteroid != null)
        {
            resolved = true;
            smallAsteroid.DestroyByProjectile();
            Destroy(gameObject);
        }
    }
}
