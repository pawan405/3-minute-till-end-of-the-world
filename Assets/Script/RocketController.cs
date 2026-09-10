using UnityEngine;

public class RocketController : MonoBehaviour
{
    public Transform target;
    public float speed = 2f;
    public GameObject explosionPrefab;

    private bool flying = false;
    private Vector3 targetPosition;

    void Start()
    {
        if (target != null)
        {
            Launch(target);
        }
    }

    public void Launch(Transform asteroid)
    {
        target = asteroid;

        // Launch ke time asteroid ki exact position save
        targetPosition = asteroid.position;

        flying = true;
    }

    void Update()
    {
        if (!flying)
            return;

        // Seedha fixed target point ki taraf
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.deltaTime
        );

        // Rocket ko target ki taraf face karao
        Vector3 direction = targetPosition - transform.position;

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                10f * Time.deltaTime
            );
        }

        // Target tak pahunch gaya
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            Explode();
        }
    }

    void Explode()
    {
        if (explosionPrefab != null)
        {
            Instantiate(
                explosionPrefab,
                targetPosition,
                Quaternion.identity
            );
        }

        if (target != null)
        {
            Destroy(target.gameObject);
        }

        Destroy(gameObject);
    }
}