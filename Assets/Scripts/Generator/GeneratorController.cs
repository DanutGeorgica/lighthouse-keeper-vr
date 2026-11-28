
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

    [Tooltip("Generator state when the scene starts.")]
    [SerializeField]
    private GeneratorState startingState = GeneratorState.Off;

    // Runtime state. Not serialized because startingState controls
    // initialization, while this variable changes during gameplay.
    private GeneratorState currentState;

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
    // DISPLAY
    // =========================================================

    [Header("Display")]
    [SerializeField] private LedDisplay ledDisplay;

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

        // Initialize the runtime state from the selected starting state.
        currentState = startingState;

        // A generator cannot run without fuel.
        // An empty tank takes priority unless the generator is faulted.
        if (currentState != GeneratorState.Faulted)
        {
            if (currentFuel <= 0f)
            {
                currentState = GeneratorState.OutOfFuel;
            }
            // OutOfFuel is not a valid state if fuel is available.
            else if (currentState == GeneratorState.OutOfFuel)
            {
                currentState = GeneratorState.Off;
            }
        }

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
        UpdateDisplay();

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
            {
                Debug.LogWarning(
                    "[Generator] Cannot start: generator is faulted!",
                    this
                );
            }

            return;
        }

        if (currentFuel <= 0f)
        {
            ChangeState(GeneratorState.OutOfFuel);

            if (enableDebugLogs)
            {
                Debug.LogWarning(
                    "[Generator] Cannot start: no fuel!",
                    this
                );
            }

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

        UpdateDisplay();

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
    // DISPLAY MANAGEMENT
    // =========================================================

    private void UpdateDisplay()
    {
        if (ledDisplay == null)
            return;

        switch (currentState)
        {
            case GeneratorState.Off:
                ledDisplay.SetLines("GENERATOR", "OFF");
                break;

            case GeneratorState.Running:
                ledDisplay.SetLines("GENERATOR", "RUNNING");
                break;

            case GeneratorState.OutOfFuel:
                ledDisplay.SetLines("WARNING", "OUT OF FUEL");
                break;

            case GeneratorState.Faulted:
                ledDisplay.SetLines("WARNING", "SYSTEM FAULT");
                break;
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
    // FUEL CONSUMPTION
    // =========================================================

    public float ConsumeFuel(float amount)
    {
        // Fuel can only be consumed while the generator is running.
        if (amount <= 0f ||
            currentFuel <= 0f ||
            currentState != GeneratorState.Running)
        {
            return 0f;
        }

        float consumedFuel = Mathf.Min(amount, currentFuel);

        currentFuel -= consumedFuel;
        currentFuel = Mathf.Max(0f, currentFuel);

        // Notify listeners about the updated fuel amount.
        FuelChanged?.Invoke(currentFuel, fuelCapacity);

        // Automatically change state when the tank becomes empty.
        if (currentFuel <= 0f)
        {
            ChangeState(GeneratorState.OutOfFuel);
        }

        return consumedFuel;
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
