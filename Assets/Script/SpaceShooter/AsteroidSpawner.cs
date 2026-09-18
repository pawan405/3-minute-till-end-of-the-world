using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class AsteroidSpawner : MonoBehaviour
{
    private const float DefaultSpawnInterval = 1.3f;

    [Header("Asteroids")]
    [SerializeField] private GameObject[] asteroidPrefabs;
    [SerializeField] private Camera playCamera;
    [SerializeField] private Transform rocket;
    [SerializeField] private Transform asteroidParent;

    [Header("Spawn Settings")]
    [SerializeField, Min(0.2f)] private float spawnInterval = DefaultSpawnInterval;
    [SerializeField, Min(1)] private int maxActiveAsteroids = 5;
    [SerializeField, Min(0.1f)] private float spawnPadding = 1.5f;
    [SerializeField, Min(0.1f)] private float asteroidSpeed = 2.4f;
    [SerializeField, Min(0f)] private float speedVariance = 0.8f;
    [SerializeField, Min(0.1f)] private float fallbackVisualScale = 0.85f;
    [SerializeField, Min(0.1f)] private float collisionRadius = 0.55f;
    [SerializeField] private Material fallbackMaterial;

    private const string FallbackShaderName = "Universal Render Pipeline/Unlit";

    private Coroutine spawnRoutine;
    private int activeAsteroidCount;
    private bool spawning;

    public int ActiveAsteroidCount => activeAsteroidCount;

    /// <summary>
    /// Connects the spawner to the active camera and rocket.
    /// </summary>
    public void Configure(Camera camera, Transform rocketTransform)
    {
        playCamera = camera;
        rocket = rocketTransform;
    }


    public void BeginSpawning()
    {
        if (spawning)
        {
            return;
        }

        spawning = true;
        spawnRoutine = StartCoroutine(SpawnRoutine());
    }

    /// <summary>
    /// Stops all future asteroid spawns immediately.
    /// </summary>
    public void StopSpawning()
    {
        spawning = false;
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private IEnumerator SpawnRoutine()
    {
        while (spawning)
        {
            if (activeAsteroidCount < maxActiveAsteroids)
            {
                SpawnAsteroid();
            }

            yield return new WaitForSeconds(spawnInterval);
        }

        spawnRoutine = null;
    }

    private void SpawnAsteroid()
    {
        if (playCamera == null || !playCamera.orthographic || asteroidPrefabs == null || asteroidPrefabs.Length == 0)
        {
            return;
        }

        GameObject prefab = asteroidPrefabs[Random.Range(0, asteroidPrefabs.Length)];
        if (prefab == null)
        {
            return;
        }

        float verticalExtent = playCamera.orthographicSize;
        float horizontalExtent = verticalExtent * playCamera.aspect;
        Vector2 targetPosition = rocket != null ? rocket.position : playCamera.transform.position;
        Vector2 spawnPosition = new Vector2(
            Random.Range(playCamera.transform.position.x - horizontalExtent, playCamera.transform.position.x + horizontalExtent),
            playCamera.transform.position.y - verticalExtent - spawnPadding);

        if (rocket != null && Vector2.Distance(spawnPosition, rocket.position) < 2f)
        {
            return;
        }

        GameObject instance = new GameObject("Asteroid", typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(Asteroid));
        instance.transform.SetParent(asteroidParent);
        instance.transform.position = spawnPosition;
        GameObject visual = Instantiate(prefab, instance.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        EnsureRenderableVisual(instance, visual);

        Rigidbody2D asteroidBody = instance.GetComponent<Rigidbody2D>();
        asteroidBody.gravityScale = 0f;
        asteroidBody.bodyType = RigidbodyType2D.Kinematic;
        asteroidBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        asteroidBody.useFullKinematicContacts = true;
        CircleCollider2D asteroidCollider = instance.GetComponent<CircleCollider2D>();
        asteroidCollider.radius = collisionRadius;

        Asteroid asteroid = instance.GetComponent<Asteroid>();
        asteroid.SetCamera(playCamera);
        Vector2 direction = (targetPosition - spawnPosition).normalized;
        float speed = asteroidSpeed + Random.Range(-speedVariance, speedVariance);
        asteroid.Initialize(direction, Mathf.Max(0.1f, speed));
        activeAsteroidCount++;
        StartCoroutine(TrackAsteroid(instance));
    }

    private void EnsureRenderableVisual(GameObject instance, GameObject importedVisual)
    {
        if (HasRenderableMesh(importedVisual))
        {
            return;
        }

        GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fallback.name = "AsteroidFallbackVisual";
        fallback.transform.SetParent(instance.transform, false);
        fallback.transform.localScale = Vector3.one * fallbackVisualScale;

        Collider fallbackCollider = fallback.GetComponent<Collider>();
        if (fallbackCollider != null)
        {
            Destroy(fallbackCollider);
        }

        Renderer fallbackRenderer = fallback.GetComponent<Renderer>();
        if (fallbackRenderer == null)
        {
            return;
        }

        if (fallbackMaterial != null)
        {
            fallbackRenderer.sharedMaterial = fallbackMaterial;
            return;
        }

        Shader fallbackShader = Shader.Find(FallbackShaderName);
        if (fallbackShader != null)
        {
            Material material = new Material(fallbackShader);
            material.color = new Color(0.26f, 0.31f, 0.38f, 1f);
            fallbackRenderer.sharedMaterial = material;
        }
    }

    private static bool HasRenderableMesh(GameObject target)
    {
        MeshFilter[] meshFilters = target.GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter meshFilter in meshFilters)
        {
            MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
            if (meshFilter.sharedMesh != null && meshRenderer != null && meshRenderer.enabled)
            {
                return true;
            }
        }

        return false;
    }


    private IEnumerator TrackAsteroid(GameObject asteroidObject)
    {
        while (asteroidObject != null)
        {
            yield return null;
        }

        activeAsteroidCount = Mathf.Max(0, activeAsteroidCount - 1);
    }
}
