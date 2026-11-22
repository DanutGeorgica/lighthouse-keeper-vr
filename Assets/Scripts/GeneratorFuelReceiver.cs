
using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class GeneratorFuelReceiver : MonoBehaviour
{
    [Header("Fuel Tank")]
    [SerializeField, Min(0.1f)]
    private float fuelCapacity = 40f;

    [SerializeField, Min(0f)]
    private float startingFuel = 0f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    [Header("Runtime Information")]
    [SerializeField] private float currentFuel;

    public float CurrentFuel => currentFuel;

    public float FuelCapacity => fuelCapacity;

    public float FuelPercentage =>
        fuelCapacity > 0f ? currentFuel / fuelCapacity : 0f;

    public bool CanAcceptFuel =>
        currentFuel < fuelCapacity;

    // Other systems can subscribe to changes in fuel.
    public event Action<float, float> FuelChanged;

    private void Awake()
    {
        currentFuel = Mathf.Clamp(
            startingFuel,
            0f,
            fuelCapacity
        );

        Collider fillZone = GetComponent<Collider>();

        if (!fillZone.isTrigger)
        {
            Debug.LogError(
                "[GeneratorFuelReceiver] FuelFillZone Collider must have Is Trigger enabled!",
                this
            );
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Find the jerrycan responsible for the overlapping collider.
        JerrycanFuelPourer jerrycan =
            other.GetComponentInParent<JerrycanFuelPourer>();

        if (jerrycan == null)
            return;

        // Only the dedicated pouring collider should transfer fuel.
        if (!jerrycan.IsPourTrigger(other))
            return;

        jerrycan.TryPourInto(this, Time.fixedDeltaTime);
    }

    private void OnTriggerExit(Collider other)
    {
        JerrycanFuelPourer jerrycan =
            other.GetComponentInParent<JerrycanFuelPourer>();

        if (jerrycan == null)
            return;

        if (!jerrycan.IsPourTrigger(other))
            return;

        jerrycan.StopPouring();
    }

    public float AddFuel(float amount)
    {
        if (amount <= 0f || !CanAcceptFuel)
            return 0f;

        float acceptedFuel = Mathf.Min(
            amount,
            fuelCapacity - currentFuel
        );

        if (acceptedFuel <= 0f)
            return 0f;

        currentFuel += acceptedFuel;

        currentFuel = Mathf.Clamp(
            currentFuel,
            0f,
            fuelCapacity
        );

        FuelChanged?.Invoke(currentFuel, fuelCapacity);

        if (enableDebugLogs)
        {
            // Avoid logging every physics frame.
            // The current amount is visible in the Inspector.
        }

        return acceptedFuel;
    }

    private void Start()
    {
        if (enableDebugLogs)
        {
            Debug.Log(
                $"[Generator] Fuel level: {currentFuel:F1} / " +
                $"{fuelCapacity:F1} L.",
                this
            );
        }
    }

    private void OnValidate()
    {
        fuelCapacity = Mathf.Max(0.1f, fuelCapacity);
        startingFuel = Mathf.Clamp(startingFuel, 0f, fuelCapacity);
    }
}
