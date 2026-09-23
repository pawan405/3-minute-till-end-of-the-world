using UnityEngine;

[DisallowMultipleComponent]
public sealed class GalaxyStarfield : MonoBehaviour
{
    private const string StarShaderName = "Universal Render Pipeline/Unlit";
    private const int DefaultStarCount = 90;
    private const float DefaultStarDepth = 0.4f;
    private const float DefaultMinimumSize = 0.025f;
    private const float DefaultMaximumSize = 0.07f;

    [SerializeField, Min(1)] private int starCount = DefaultStarCount;
    [SerializeField, Min(0.01f)] private float starDepth = DefaultStarDepth;
    [SerializeField, Min(0.001f)] private float minimumSize = DefaultMinimumSize;
    [SerializeField, Min(0.001f)] private float maximumSize = DefaultMaximumSize;
    [SerializeField] private Color starColor = new Color(0.65f, 0.9f, 1f, 0.8f);

    private ParticleSystem starParticles;

    private void Awake()
    {
        CreateStars();
    }

    /// <summary>
    /// Creates a small, static galaxy star layer behind the rocket and asteroids.
    /// </summary>
    public void CreateStars()
    {
        if (starParticles != null || starCount <= 0)
        {
            return;
        }

        starParticles = gameObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = starParticles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = 10000f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(minimumSize, maximumSize);
        main.startColor = starColor;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = starCount;

        ParticleSystem.EmissionModule emission = starParticles.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = starParticles.shape;
        shape.enabled = false;

        ParticleSystemRenderer renderer = starParticles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = -50;

        Shader shader = Shader.Find(StarShaderName);
        if (shader != null)
        {
            Material material = new Material(shader);
            material.color = starColor;
            renderer.sharedMaterial = material;
        }

        Camera targetCamera = Camera.main;
        float height = targetCamera != null && targetCamera.orthographic ? targetCamera.orthographicSize : 5.5f;
        float width = targetCamera != null && targetCamera.orthographic ? height * targetCamera.aspect : 9f;
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[starCount];
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].position = new Vector3(
                Random.Range(-width, width),
                Random.Range(-height, height),
                starDepth);
            particles[i].startSize = Random.Range(minimumSize, maximumSize);
            particles[i].startColor = starColor;
            particles[i].startLifetime = 10000f;
            particles[i].remainingLifetime = 10000f;
        }

        starParticles.SetParticles(particles, particles.Length);
        starParticles.Play();
    }
}
