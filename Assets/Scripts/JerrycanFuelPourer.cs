
using UnityEngine;

[DisallowMultipleComponent]
public class JerrycanFuelPourer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Collider pourTrigger;
    [SerializeField] private ParticleSystem fuelParticles;

    [Header("Pour Detection")]
    [Tooltip("Local direction that points towards the top of the upright jerrycan.")]
    [SerializeField] private Vector3 localUpDirection = Vector3.up;

    [Tooltip("How far the jerrycan must be tilted upside down before pouring.")]
    [SerializeField, Range(0f, 1f)]
    private float upsideDownThreshold = 0.7f;

    [Header("Fuel")]
    [SerializeField, Min(0f)]
    private float startingFuel = 20f;

    [Tooltip("Fuel transferred per second, in litres.")]
    [SerializeField, Min(0f)]
    private float pourRate = 2.5f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    [Header("Runtime Information")]
    [SerializeField] private float remainingFuel;

    private bool isPouring;

    public float RemainingFuel => remainingFuel;

    public bool IsEmpty => remainingFuel <= 0.001f;

    public bool IsUpsideDown
    {
        get
        {
            Vector3 worldUp = transform.TransformDirection(
                localUpDirection.normalized
            );

            float upsideDownAmount = Vector3.Dot(
                worldUp,
                Vector3.down
            );

            return upsideDownAmount >= upsideDownThreshold;
        }
    }

    private void Awake()
    {
        remainingFuel = startingFuel;

        if (fuelParticles != null)
        {
            fuelParticles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }
    }

    public bool IsPourTrigger(Collider other)
    {
        return other != null && other == pourTrigger;
    }

    public void TryPourInto(
        GeneratorFuelReceiver generator,
        float deltaTime)
    {
        if (generator == null ||
            !IsUpsideDown ||
            IsEmpty ||
            !generator.CanAcceptFuel ||
            pourRate <= 0f)
        {
            StopPouring();
            return;
        }

        // Calculate how much fuel can be transferred this frame.
        float requestedFuel = Mathf.Min(
            pourRate * deltaTime,
            remainingFuel
        );

        // Ask the generator to accept the fuel.
        float acceptedFuel = generator.AddFuel(requestedFuel);

        if (acceptedFuel <= 0f)
        {
            StopPouring();
            return;
        }

        // Remove transferred fuel from the jerrycan.
        remainingFuel -= acceptedFuel;

        remainingFuel = Mathf.Max(0f, remainingFuel);

        // Start the visual particle effect.
        if (fuelParticles != null && !fuelParticles.isPlaying)
        {
            fuelParticles.Play();
        }

        if (!isPouring)
        {
            isPouring = true;

            if (enableDebugLogs)
            {
                Debug.Log(
                    "[Jerrycan] Started pouring fuel.",
                    this
                );
            }
        }

        if (IsEmpty || !generator.CanAcceptFuel)
        {
            StopPouring();
        }
    }

    public void StopPouring()
    {
        if (fuelParticles != null && fuelParticles.isPlaying)
        {
            fuelParticles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmitting
            );
        }

        if (isPouring)
        {
            isPouring = false;

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[Jerrycan] Pouring stopped. Remaining fuel: " +
                    $"{remainingFuel:F1} L.",
                    this
                );
            }
        }
    }

    private void OnDisable()
    {
        StopPouring();
    }

    private void OnValidate()
    {
        startingFuel = Mathf.Max(0f, startingFuel);
        pourRate = Mathf.Max(0f, pourRate);

        if (localUpDirection.sqrMagnitude < 0.001f)
            localUpDirection = Vector3.up;
    }
}
