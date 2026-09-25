using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RocketAsteroidInteraction : MonoBehaviour
{
    private const float DefaultLaunchLift = 1.4f;
    private const float DefaultArcHeight = 1.2f;
    private const float DefaultLiftDuration = 0.55f;
    private const float DefaultFlightDuration = 2.2f;
    private const float DefaultImpactPause = 0.12f;
    private const float DefaultExplosionLifetime = 6f;

    [Header("Interaction")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private string playerTag = "Player";

    [Header("Rocket Strike")]
    [SerializeField] private Transform rocket;
    [SerializeField] private Transform asteroid;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private GameObject rocketTrailPrefab;
    [SerializeField, Min(0.1f)] private float launchLift = DefaultLaunchLift;
    [SerializeField, Min(0f)] private float arcHeight = DefaultArcHeight;
    [SerializeField, Min(0.1f)] private float liftDuration = DefaultLiftDuration;
    [SerializeField, Min(0.1f)] private float flightDuration = DefaultFlightDuration;
    [SerializeField, Min(0f)] private float impactPause = DefaultImpactPause;
    [SerializeField, Min(0.1f)] private float explosionLifetime = DefaultExplosionLifetime;

    private bool playerInRange;
    private bool sequenceStarted;
    private Collider interactionCollider;
    private GameObject trailInstance;

    private void Awake()
    {
        interactionCollider = GetComponent<Collider>();
        if (interactionCollider != null)
        {
            interactionCollider.isTrigger = true;
        }
    }

    private void Update()
    {
        if (playerInRange && !sequenceStarted && Input.GetKeyDown(interactionKey))
        {
            StartCoroutine(LaunchRocketRoutine());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
        }
    }

    private IEnumerator LaunchRocketRoutine()
    {
        sequenceStarted = true;
        playerInRange = false;

        if (interactionCollider != null)
        {
            interactionCollider.enabled = false;
        }

        if (rocket == null || asteroid == null)
        {
            yield break;
        }

        Vector3 startPosition = rocket.position;
        Quaternion startRotation = rocket.rotation;
        Vector3 targetPosition = asteroid.position;
        Vector3 launchPosition = startPosition + Vector3.up * launchLift;
        Vector3 arcControl = Vector3.Lerp(launchPosition, targetPosition, 0.5f) + Vector3.up * arcHeight;

        AttachRocketTrail();

        yield return AnimateRocketSegment(startPosition, launchPosition, startRotation, liftDuration);

        float elapsed = 0f;
        while (elapsed < flightDuration && rocket != null)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / flightDuration);
            float easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);
            Vector3 previousPosition = rocket.position;
            rocket.position = QuadraticBezier(launchPosition, arcControl, targetPosition, easedTime);
            RotateRocket(previousPosition, rocket.position, startRotation, easedTime);
            yield return null;
        }

        if (rocket == null)
        {
            yield break;
        }

        rocket.position = targetPosition;
        yield return new WaitForSecondsRealtime(impactPause);
        PlayImpact(targetPosition);

        if (asteroid != null)
        {
            asteroid.gameObject.SetActive(false);
        }

        if (rocket != null)
        {
            rocket.gameObject.SetActive(false);
        }
    }

    private IEnumerator AnimateRocketSegment(Vector3 startPosition, Vector3 endPosition, Quaternion startRotation, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && rocket != null)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);
            Vector3 previousPosition = rocket.position;
            rocket.position = Vector3.Lerp(startPosition, endPosition, easedTime);
            RotateRocket(previousPosition, rocket.position, startRotation, easedTime);
            yield return null;
        }
    }

    private void RotateRocket(Vector3 previousPosition, Vector3 currentPosition, Quaternion startRotation, float blend)
    {
        Vector3 direction = currentPosition - previousPosition;
        if (direction.sqrMagnitude < 0.0001f || rocket == null)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized) * startRotation;
        rocket.rotation = Quaternion.Slerp(rocket.rotation, targetRotation, Mathf.Clamp01(Time.deltaTime * 8f + blend * 0.08f));
    }

    private void AttachRocketTrail()
    {
        if (rocketTrailPrefab == null || rocket == null)
        {
            return;
        }

        trailInstance = Instantiate(rocketTrailPrefab, rocket);
        trailInstance.transform.localPosition = Vector3.zero;
        trailInstance.transform.localRotation = Quaternion.identity;
    }

    private void PlayImpact(Vector3 position)
    {
        if (trailInstance != null)
        {
            trailInstance.SetActive(false);
        }

        if (explosionPrefab == null)
        {
            return;
        }

        GameObject explosion = Instantiate(explosionPrefab, position, Quaternion.identity);
        explosion.transform.localScale = Vector3.one * 1.5f;
        Destroy(explosion, explosionLifetime);
    }

    private static Vector3 QuadraticBezier(Vector3 start, Vector3 control, Vector3 end, float t)
    {
        float inverseTime = 1f - t;
        return inverseTime * inverseTime * start + 2f * inverseTime * t * control + t * t * end;
    }
}
