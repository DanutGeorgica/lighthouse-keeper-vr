
using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class GeneratorController : MonoBehaviour
{
    // =========================================================
    // GENERATOR STATE
    // =========================================================

    public enum GeneratorState
    {
        Off,
        Running,
        OutOfFuel,
        Faulted
    }

    [Header("Generator State")]
    [SerializeField]
    private GeneratorState currentState = GeneratorState.Off;

    public GeneratorState CurrentState => currentState;

    public bool IsRunning =>
        currentState == GeneratorState.Running;

    public bool IsFaulted =>
        currentState == GeneratorState.Faulted;

    public bool CanStart =>
        currentState == GeneratorState.Off && currentFuel > 0f;

    // =========================================================
    // FUEL TANK
    // =========================================================

    [Header("Fuel Tank")]
    [SerializeField, Min(0.1f)]
    private float fuelCapacity = 40f;

    [SerializeField, Min(0f)]
    private float startingFuel = 0f;

    [Header("Runtime Information")]
    [SerializeField]
    private float currentFuel;

    public float CurrentFuel => currentFuel;

    public float FuelCapacity => fuelCapacity;

    public float FuelPercentage =>
        fuelCapacity > 0f ? currentFuel / fuelCapacity : 0f;

    public bool CanAcceptFuel =>
        currentFuel < fuelCapacity;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]
    [SerializeField]
    private bool enableDebugLogs = true;

    // =========================================================
    // EVENTS
    // =========================================================

    public event Action<float, float> FuelChanged;

    public event Action<GeneratorState> StateChanged;

    // =========================================================
    // INITIALIZATION
    // =========================================================

    private void Awake()
    {
        currentFuel = Mathf.Clamp(
            startingFuel,
            0f,
            fuelCapacity
        );

        // Start OFF if fuel is available.
        // Otherwise, start OUT OF FUEL.
        currentState = currentFuel > 0f
            ? GeneratorState.Off
            : GeneratorState.OutOfFuel;

        Collider fillZone = GetComponent<Collider>();

        if (!fillZone.isTrigger)
        {
            Debug.LogError(
                "[Generator] FuelFillZone Collider must have Is Trigger enabled!",
                this
            );
        }
    }

    private void Start()
    {
        if (enableDebugLogs)
        {
            Debug.Log(
                $"[Generator] Initial state: {currentState}. " +
                $"Fuel: {currentFuel:F1} / {fuelCapacity:F1} L.",
                this
            );
        }
    }

    // =========================================================
    // GENERATOR OPERATIONS
    // =========================================================

    public void StartGenerator()
    {
        if (currentState == GeneratorState.Running)
            return;

        if (currentState == GeneratorState.Faulted)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[Generator] Cannot start: generator is faulted!", this);

            return;
        }

        if (currentFuel <= 0f)
        {
            ChangeState(GeneratorState.OutOfFuel);

            if (enableDebugLogs)
                Debug.LogWarning("[Generator] Cannot start: no fuel!", this);

            return;
        }

        if (currentState != GeneratorState.Off)
            return;

        ChangeState(GeneratorState.Running);
    }

    public void StopGenerator()
    {
        if (currentState != GeneratorState.Running)
            return;

        ChangeState(GeneratorState.Off);
    }

    // Used by future repair mechanics.
    public void SetGeneratorFaulted(bool faulted)
    {
        if (faulted)
        {
            ChangeState(GeneratorState.Faulted);
        }
        else if (currentState == GeneratorState.Faulted)
        {
            ChangeState(
                currentFuel > 0f
                    ? GeneratorState.Off
                    : GeneratorState.OutOfFuel
            );
        }
    }

    // =========================================================
    // STATE MANAGEMENT
    // =========================================================

    private void ChangeState(GeneratorState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;

        StateChanged?.Invoke(currentState);

        if (enableDebugLogs)
        {
            Debug.Log(
                $"[Generator] State changed to: {currentState}.",
                this
            );
        }
    }

    // =========================================================
    // FUEL TRANSFER
    // =========================================================

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

        // Refuelling an empty generator makes it available again.
        // It does NOT automatically start it.
        if (currentState == GeneratorState.OutOfFuel && currentFuel > 0f)
        {
            ChangeState(GeneratorState.Off);
        }

        FuelChanged?.Invoke(currentFuel, fuelCapacity);

        return acceptedFuel;
    }

    // =========================================================
    // INSPECTOR VALIDATION
    // =========================================================

    private void OnValidate()
    {
        fuelCapacity = Mathf.Max(0.1f, fuelCapacity);

        startingFuel = Mathf.Clamp(
            startingFuel,
            0f,
            fuelCapacity
        );
    }
}
