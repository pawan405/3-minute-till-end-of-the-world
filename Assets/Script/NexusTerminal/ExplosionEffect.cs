using UnityEngine;

[DisallowMultipleComponent]
public sealed class ExplosionEffect : MonoBehaviour
{
    [Header("Effect Prefabs")]
    [SerializeField] private GameObject smallExplosionPrefab;
    [SerializeField] private GameObject launchExplosionPrefab;
    [SerializeField, Min(0.1f)] private float effectLifetime = 4f;
    [SerializeField] private Camera effectCamera;
    [SerializeField, Min(0f)] private float cameraShakeDuration = 0.18f;
    [SerializeField, Min(0f)] private float cameraShakeMagnitude = 0.08f;

    private Vector3 cameraBasePosition;
    private float shakeRemaining;

    private void LateUpdate()
    {
        if (effectCamera == null || shakeRemaining <= 0f)
        {
            return;
        }

        shakeRemaining -= Time.unscaledDeltaTime;
        effectCamera.transform.position = cameraBasePosition + Random.insideUnitSphere * cameraShakeMagnitude;
        if (shakeRemaining <= 0f)
        {
            effectCamera.transform.position = cameraBasePosition;
        }
    }

    /// <summary>
    /// Instantiates the selected explosion prefab at a world position.
    /// </summary>
    public void PlayAt(Vector3 position, Quaternion rotation)
    {
        SpawnEffect(smallExplosionPrefab, position, rotation);
    }

    /// <summary>
    /// Plays the short explosion used for the first asteroid impact.
    /// </summary>
    public void PlayRocketImpact(Vector3 position)
    {
        SpawnEffect(smallExplosionPrefab, position, Quaternion.identity);
        BeginCameraShake();
    }

    /// <summary>
    /// Plays an optional launch climax effect at the rocket transform.
    /// </summary>
    public void PlayLaunchEffect(Transform rocket)
    {
        if (rocket == null)
        {
            return;
        }

        SpawnEffect(launchExplosionPrefab, rocket.position, rocket.rotation);
        BeginCameraShake();
    }

    private void SpawnEffect(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            return;
        }

        GameObject instance = Instantiate(prefab, position, rotation);
        Destroy(instance, effectLifetime);
    }

    private void BeginCameraShake()
    {
        if (effectCamera == null || cameraShakeDuration <= 0f || cameraShakeMagnitude <= 0f)
        {
            return;
        }

        cameraBasePosition = effectCamera.transform.position;
        shakeRemaining = cameraShakeDuration;
    }
}
