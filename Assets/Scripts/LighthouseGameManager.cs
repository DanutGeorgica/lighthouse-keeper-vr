using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class LighthouseGameManager : MonoBehaviour
{
    public static LighthouseGameManager Instance { get; private set; }

    // =========================================================
    // BEACON
    // =========================================================

    [Header("Beacon")]
    [SerializeField] private LighthouseBeacon lighthouseBeacon;
    [SerializeField] private bool beaconStartsOn = false;
    [SerializeField] private int beaconOnPosition = 1;

    [Header("Beacon Speed Knob")]
    [SerializeField, Range(0, 6)]
    private int beaconSpeedStartsAt = 3;

    [Header("Beacon Runtime Information")]
    [SerializeField] private bool isBeaconPowered;

    // =========================================================
    // FOGHORN
    // =========================================================

    [Header("Foghorn")]
    [SerializeField] private FoghornController foghornController;

    // =========================================================
    // GENERATOR
    // =========================================================

    [Header("Generator")]
    [SerializeField] private GeneratorController generatorController;

    [Header("Fuel Consumption (Litres per Minute)")]
    [Tooltip("Fuel consumed by the running generator itself, even when no powered equipment is active.")]
    [SerializeField, Min(0f)]
    private float generatorIdleFuelConsumptionPerMinute = 0.1f;

    [Tooltip("Additional fuel consumed while the Fresnel lens is switched ON and powered.")]
    [SerializeField, Min(0f)]
    private float beaconFuelConsumptionPerMinute = 0.3f;

    [Tooltip("Additional fuel consumed while the interior lights are switched ON and powered.")]
    [SerializeField, Min(0f)]
    private float interiorLightsFuelConsumptionPerMinute = 0.1f;

    [Tooltip("The generator is considered low on fuel below this percentage.")]
    [SerializeField, Range(0f, 1f)]
    private float generatorLowFuelThreshold = 0.25f;

    [Header("Generator Runtime Information")]
    [SerializeField]
    private GeneratorController.GeneratorState currentGeneratorState;

    [SerializeField]
    private float currentGeneratorFuel;

    [SerializeField]
    private float currentGeneratorFuelPercentage;

    [SerializeField]
    private bool isGeneratorLowOnFuel;

    [SerializeField]
    private float currentGeneratorFuelConsumptionPerMinute;

    // =========================================================
    // INTERIOR LIGHTS
    // =========================================================

    [Header("Interior Lights")]
    [SerializeField] private Light[] interiorLights;

    [Header("Interior Lights Runtime Information")]
    [SerializeField] private bool areInteriorLightsPowered;

    // =========================================================
    // RADIO POWER
    // =========================================================

    [Header("Radio Power")]
    [SerializeField] private bool radioStartsOn = false;

    [Tooltip("The indicator light on the physical radio power switch.")]
    [SerializeField] private Light radioPowerIndicatorLight;

    // =========================================================
    // RADIO FREQUENCY
    // =========================================================

    [Header("Radio Frequency")]
    [SerializeField, Range(0, 6)]
    private int radioFrequencyStartsAt = 3;

    [Tooltip("Percentage chance of corrupting each character per frequency step of error.")]
    [SerializeField, Range(0f, 0.25f)]
    private float corruptionChancePerFrequencyStep = 0.10f;

    // =========================================================
    // RADIO UI
    // =========================================================

    [Header("Radio UI")]
    [SerializeField] private GameObject radioMessagePanel;
    [SerializeField] private TMP_Text radioMessageText;
    [SerializeField] private TMP_Text radioFrequencyText;
    [SerializeField] private TMP_Text[] radioResponseOptionTexts;

    [Header("Radio UI Colors")]
    [SerializeField] private Color normalResponseColor = Color.white;
    [SerializeField] private Color selectedResponseColor = Color.yellow;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool queueMockRadioMessagesOnStart = true;

    // =========================================================
    // GAMEPLAY STATES
    // =========================================================

    // These two states represent the player's switch/lever positions.
    // They are intentionally retained during a generator power outage.
    public bool IsBeaconOn { get; private set; }

    public bool IsBeaconPowered => isBeaconPowered;

    public int CurrentBeaconSpeedStep { get; private set; } = 3;

    public bool AreInteriorLightsOn { get; private set; }

    public bool AreInteriorLightsPowered => areInteriorLightsPowered;

    // Generator state and fuel information.
    public GeneratorController.GeneratorState CurrentGeneratorState =>
        currentGeneratorState;

    public bool IsGeneratorRunning =>
        currentGeneratorState == GeneratorController.GeneratorState.Running;

    public float CurrentGeneratorFuel => currentGeneratorFuel;

    public float GeneratorFuelCapacity { get; private set; }

    /// <summary>
    /// Normalized fuel percentage, from 0 to 1.
    /// Multiply by 100 to get a percentage from 0 to 100.
    /// </summary>
    public float GeneratorFuelPercentage => currentGeneratorFuelPercentage;

    public bool IsGeneratorLowOnFuel => isGeneratorLowOnFuel;

    /// <summary>
    /// Total current fuel consumption, including generator idle consumption
    /// and all equipment currently powered by the generator.
    /// </summary>
    public float GeneratorFuelConsumptionPerMinute =>
        currentGeneratorFuelConsumptionPerMinute;

    public bool IsRadioOn { get; private set; }

    public int CurrentRadioFrequency { get; private set; } = 3;

    public string CurrentRadioMessage { get; private set; }

    public int RequiredRadioFrequency { get; private set; }

    public bool IsRadioFrequencyCorrect =>
        CurrentRadioMessage != null &&
        CurrentRadioFrequency == RequiredRadioFrequency;

    public int QueuedRadioMessageCount => pendingRadioMessages.Count;

    public string[] CurrentRadioResponseOptions { get; private set; }
        = Array.Empty<string>();

    public int SelectedRadioResponseIndex { get; private set; } = -1;

    public string SelectedRadioResponse =>
        SelectedRadioResponseIndex >= 0 &&
        SelectedRadioResponseIndex < CurrentRadioResponseOptions.Length
            ? CurrentRadioResponseOptions[SelectedRadioResponseIndex]
            : null;

    private bool radioResponseAlreadySubmitted;

    // Stores the currently distorted message and response options.
    // Prevents random characters from changing when unrelated UI updates occur.
    private string displayedRadioMessage = "";
    private string[] displayedRadioResponseOptions = Array.Empty<string>();

    // =========================================================
    // RADIO MESSAGE QUEUE
    // =========================================================

    private class RadioMessageData
    {
        public string Message;
        public int RequiredFrequency;
        public string[] ResponseOptions;

        public RadioMessageData(
            string message,
            int requiredFrequency,
            string[] responseOptions)
        {
            Message = message;
            RequiredFrequency = requiredFrequency;
            ResponseOptions = (string[])responseOptions.Clone();
        }
    }

    private readonly Queue<RadioMessageData> pendingRadioMessages =
        new Queue<RadioMessageData>();

    // =========================================================
    // EVENTS
    // =========================================================

    // These events report the player's requested switch/lever states.
    public event Action<bool> BeaconPowerChanged;
    public event Action<int> BeaconSpeedChanged;
    public event Action<bool> InteriorLightsChanged;

    // These events report whether the equipment is actually receiving power.
    public event Action<bool> BeaconPoweredChanged;
    public event Action<bool> InteriorLightsPoweredChanged;

    public event Action FoghornTriggered;

    // Generator events forwarded from GeneratorController.
    public event Action<GeneratorController.GeneratorState> GeneratorStateChanged;
    public event Action<float, float> GeneratorFuelChanged;
    public event Action<bool> GeneratorLowFuelChanged;

    public event Action<bool> RadioPowerChanged;
    public event Action<int> RadioFrequencyChanged;

    public event Action<string, string[]> RadioMessageReceived;
    public event Action<int> RadioResponseSelectionChanged;
    public event Action<string> RadioResponseSubmitted;

    // =========================================================
    // INITIALIZATION
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError(
                "[GameManager] Another LighthouseGameManager already exists!",
                this
            );

            enabled = false;
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // Initialize generator tracking before the other gameplay systems.
        InitializeGeneratorTracking();

        // Beacon startup configuration.
        ApplyBeaconSpeedStep(beaconSpeedStartsAt, false);
        ApplyBeaconState(beaconStartsOn, false);

        // Interior lights always start ON as a requested switch state.
        // They only illuminate when the generator is actually running.
        ApplyInteriorLights(true, false);

        // Recalculate the total load after all startup switch states are applied.
        RecalculateGeneratorFuelConsumption();

        // Radio startup configuration.
        ApplyRadioFrequency(radioFrequencyStartsAt, false);
        ApplyRadioPower(radioStartsOn, false);

        // Always display the radio interface.
        UpdateRadioUI();

        // Queue test transmissions.
        if (queueMockRadioMessagesOnStart)
        {
            QueueMockRadioMessages();
        }
    }

    private void Update()
    {
        UpdateGeneratorFuelConsumption();
    }

    private void OnDestroy()
    {
        // Unsubscribe from generator events to avoid stale event subscriptions.
        if (generatorController != null)
        {
            generatorController.StateChanged -= HandleGeneratorStateChanged;
            generatorController.FuelChanged -= HandleGeneratorFuelChanged;
        }

        if (Instance == this)
            Instance = null;
    }

    // =========================================================
    // GENERATOR INITIALIZATION
    // =========================================================

    private void InitializeGeneratorTracking()
    {
        if (generatorController == null)
        {
            Debug.LogWarning(
                "[GameManager] GeneratorController reference is missing! " +
                "Generator tracking and fuel consumption are disabled.",
                this
            );

            return;
        }

        // Subscribe to the generator's C# events.
        generatorController.StateChanged += HandleGeneratorStateChanged;
        generatorController.FuelChanged += HandleGeneratorFuelChanged;

        // Read the initial values. GeneratorController initializes its fuel
        // and state in Awake(), so these values are ready by this point.
        currentGeneratorState = generatorController.CurrentState;
        currentGeneratorFuel = generatorController.CurrentFuel;
        GeneratorFuelCapacity = generatorController.FuelCapacity;

        currentGeneratorFuelPercentage = GeneratorFuelCapacity > 0f
            ? Mathf.Clamp01(currentGeneratorFuel / GeneratorFuelCapacity)
            : 0f;

        // Establish the initial low-fuel status without firing an event.
        isGeneratorLowOnFuel =
            currentGeneratorFuelPercentage < generatorLowFuelThreshold;

        if (enableDebugLogs)
        {
            Debug.Log(
                $"[GameManager] Generator tracking initialized. " +
                $"State: {currentGeneratorState}. " +
                $"Fuel: {currentGeneratorFuel:F1}/{GeneratorFuelCapacity:F1} L. " +
                $"Low fuel: {isGeneratorLowOnFuel}.",
                this
            );
        }
    }

    // =========================================================
    // GENERATOR EVENT HANDLERS
    // =========================================================

    private void HandleGeneratorStateChanged(
        GeneratorController.GeneratorState newState)
    {
        currentGeneratorState = newState;

        // The switch states remain unchanged. Only the actual power supplied
        // to the beacon and interior lights changes with generator availability.
        ApplyGeneratorPowerToConsumers();
        RecalculateGeneratorFuelConsumption();

        GeneratorStateChanged?.Invoke(newState);

        if (enableDebugLogs)
        {
            Debug.Log(
                $"[GameManager] Generator state updated: {newState}.",
                this
            );
        }
    }

    private void HandleGeneratorFuelChanged(
        float currentFuel,
        float fuelCapacity)
    {
        bool wasLowOnFuel = isGeneratorLowOnFuel;

        currentGeneratorFuel = currentFuel;
        GeneratorFuelCapacity = fuelCapacity;

        currentGeneratorFuelPercentage = fuelCapacity > 0f
            ? Mathf.Clamp01(currentFuel / fuelCapacity)
            : 0f;

        isGeneratorLowOnFuel =
            currentGeneratorFuelPercentage < generatorLowFuelThreshold;

        // Forward the fuel update to any other interested gameplay systems.
        GeneratorFuelChanged?.Invoke(currentFuel, fuelCapacity);

        // Only notify listeners when crossing the low-fuel threshold.
        if (wasLowOnFuel != isGeneratorLowOnFuel)
        {
            GeneratorLowFuelChanged?.Invoke(isGeneratorLowOnFuel);

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[GameManager] Generator low-fuel status changed to " +
                    $"{isGeneratorLowOnFuel}. " +
                    $"Fuel: {currentGeneratorFuelPercentage * 100f:F1}%.",
                    this
                );
            }
        }
    }

    // =========================================================
    // GENERATOR FUEL CONSUMPTION
    // =========================================================

    private void UpdateGeneratorFuelConsumption()
    {
        if (generatorController == null ||
            !generatorController.IsRunning ||
            currentGeneratorFuelConsumptionPerMinute <= 0f)
        {
            return;
        }

        // Convert the current combined load from litres/minute to the amount
        // used during this frame. The rate is recalculated whenever a load
        // or the generator state changes.
        float fuelToConsume =
            currentGeneratorFuelConsumptionPerMinute / 60f * Time.deltaTime;

        generatorController.ConsumeFuel(fuelToConsume);
    }

    private void RecalculateGeneratorFuelConsumption()
    {
        float totalConsumption = 0f;

        // A running generator consumes some fuel even with no equipment on.
        if (IsGeneratorRunning)
        {
            totalConsumption += generatorIdleFuelConsumptionPerMinute;

            if (isBeaconPowered)
                totalConsumption += beaconFuelConsumptionPerMinute;

            if (areInteriorLightsPowered)
                totalConsumption += interiorLightsFuelConsumptionPerMinute;
        }

        bool consumptionChanged = !Mathf.Approximately(
            currentGeneratorFuelConsumptionPerMinute,
            totalConsumption
        );

        currentGeneratorFuelConsumptionPerMinute = totalConsumption;

        if (consumptionChanged && enableDebugLogs)
        {
            Debug.Log(
                $"[GameManager] Total generator fuel consumption: " +
                $"{currentGeneratorFuelConsumptionPerMinute:F3} L/min.",
                this
            );
        }
    }

    // =========================================================
    // BEACON POWER
    // =========================================================

    public void HandleBeaconKeyPosition(int position)
    {
        SetBeaconPower(position == beaconOnPosition);
    }

    public void SetBeaconPower(bool powered)
    {
        ApplyBeaconState(powered, true);
    }

    public void ToggleBeacon()
    {
        SetBeaconPower(!IsBeaconOn);
    }

    private void ApplyBeaconState(bool powered, bool notify)
    {
        bool stateChanged = IsBeaconOn != powered;

        // Remember the physical key-switch position, even without generator power.
        IsBeaconOn = powered;

        ApplyBeaconOutputPower();
        RecalculateGeneratorFuelConsumption();

        if (notify && stateChanged)
            BeaconPowerChanged?.Invoke(powered);

        if (enableDebugLogs)
        {
            Debug.Log(
                $"[GameManager] Beacon switch is now {(powered ? "ON" : "OFF")}. " +
                $"Actual power: {(isBeaconPowered ? "ON" : "OFF")}.",
                this
            );
        }
    }

    private void ApplyBeaconOutputPower()
    {
        bool shouldBePowered = IsBeaconOn && IsGeneratorRunning;
        bool powerStateChanged = isBeaconPowered != shouldBePowered;

        isBeaconPowered = shouldBePowered;

        if (lighthouseBeacon != null)
        {
            // Apply even when the value has not changed, ensuring the component
            // is synchronized during scene initialization as well.
            lighthouseBeacon.SetPowered(shouldBePowered);
        }
        else if (IsBeaconOn)
        {
            Debug.LogError(
                "[GameManager] LighthouseBeacon reference is missing!",
                this
            );
        }

        if (powerStateChanged)
        {
            BeaconPoweredChanged?.Invoke(isBeaconPowered);

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[GameManager] Beacon actual power is now " +
                    $"{(isBeaconPowered ? "ON" : "OFF")}.",
                    this
                );
            }
        }
    }

    // =========================================================
    // BEACON ROTATION SPEED
    // =========================================================

    public void HandleBeaconSpeedPosition(int position)
    {
        SetBeaconSpeedStep(position);
    }

    public void SetBeaconSpeedStep(int step)
    {
        ApplyBeaconSpeedStep(Mathf.Clamp(step, 0, 6), true);
    }

    private void ApplyBeaconSpeedStep(int step, bool notify)
    {
        bool stateChanged = CurrentBeaconSpeedStep != step;

        CurrentBeaconSpeedStep = step;

        if (lighthouseBeacon != null)
        {
            lighthouseBeacon.SetRotationSpeedStep(step);
        }
        else
        {
            Debug.LogError(
                "[GameManager] LighthouseBeacon reference is missing!",
                this
            );
        }

        if (notify && stateChanged)
            BeaconSpeedChanged?.Invoke(step);

        if (enableDebugLogs)
        {
            Debug.Log(
                $"[GameManager] Beacon speed set to step {step + 1}.",
                this
            );
        }
    }

    // =========================================================
    // FOGHORN
    // =========================================================

    public void TriggerFoghorn()
    {
        if (foghornController == null)
        {
            Debug.LogWarning(
                "[GameManager] FoghornController reference is missing!",
                this
            );

            return;
        }

        bool played = foghornController.PlayFoghorn();

        if (!played)
            return;

        FoghornTriggered?.Invoke();

        if (enableDebugLogs)
        {
            Debug.Log("[GameManager] Foghorn activated.", this);
        }
    }

    // =========================================================
    // INTERIOR LIGHTS
    // =========================================================

    public void SetInteriorLights(bool powered)
    {
        ApplyInteriorLights(powered, true);
    }

    private void ApplyInteriorLights(bool powered, bool notify)
    {
        bool stateChanged = AreInteriorLightsOn != powered;

        // Remember the physical lever position during a generator outage.
        AreInteriorLightsOn = powered;

        ApplyInteriorLightsOutputPower();
        RecalculateGeneratorFuelConsumption();

        if (notify && stateChanged)
            InteriorLightsChanged?.Invoke(powered);

        if (enableDebugLogs)
        {
            Debug.Log(
                $"[GameManager] Interior light switch is now " +
                $"{(powered ? "ON" : "OFF")}. " +
                $"Actual power: {(areInteriorLightsPowered ? "ON" : "OFF")}.",
                this
            );
        }
    }

    private void ApplyInteriorLightsOutputPower()
    {
        bool shouldBePowered = AreInteriorLightsOn && IsGeneratorRunning;
        bool powerStateChanged = areInteriorLightsPowered != shouldBePowered;

        areInteriorLightsPowered = shouldBePowered;

        if (interiorLights != null)
        {
            foreach (Light interiorLight in interiorLights)
            {
                if (interiorLight != null)
                    interiorLight.enabled = shouldBePowered;
            }
        }

        if (powerStateChanged)
        {
            InteriorLightsPoweredChanged?.Invoke(areInteriorLightsPowered);

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[GameManager] Interior lights actual power is now " +
                    $"{(areInteriorLightsPowered ? "ON" : "OFF")}.",
                    this
                );
            }
        }
    }

    private void ApplyGeneratorPowerToConsumers()
    {
        ApplyBeaconOutputPower();
        ApplyInteriorLightsOutputPower();
    }

    // =========================================================
    // RADIO POWER
    // =========================================================

    public void SetRadioPower(bool powered)
    {
        ApplyRadioPower(powered, true);
    }

    private void ApplyRadioPower(bool powered, bool notify)
    {
        bool stateChanged = IsRadioOn != powered;

        IsRadioOn = powered;

        // Update physical power indicator.
        if (radioPowerIndicatorLight != null)
            radioPowerIndicatorLight.enabled = powered;

        // Display a queued message when power becomes available.
        if (powered)
            DisplayNextRadioMessage();
        else
            UpdateRadioUI();

        if (notify && stateChanged)
            RadioPowerChanged?.Invoke(powered);

        if (enableDebugLogs)
        {
            Debug.Log(
                $"[GameManager] Radio is now {(powered ? "ON" : "OFF")}.",
                this
            );
        }
    }

    // =========================================================
    // RADIO FREQUENCY
    // =========================================================

    public void SetRadioFrequency(int frequency)
    {
        ApplyRadioFrequency(Mathf.Clamp(frequency, 0, 6), true);
    }

    private void ApplyRadioFrequency(int frequency, bool notify)
    {
        bool stateChanged = CurrentRadioFrequency != frequency;

        CurrentRadioFrequency = frequency;

        // Regenerate all distorted radio text only when frequency changes.
        RegenerateDisplayedRadioText();

        UpdateRadioUI();

        if (notify && stateChanged)
            RadioFrequencyChanged?.Invoke(frequency);

        if (enableDebugLogs)
        {
            Debug.Log(
                $"[GameManager] Radio frequency set to {frequency}.",
                this
            );
        }
    }

    // =========================================================
    // RADIO INCOMING MESSAGES
    // =========================================================

    public void ReceiveRadioMessage(
        string message,
        int requiredFrequency,
        string[] responseOptions)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            Debug.LogWarning(
                "[GameManager] Received an empty radio message!",
                this
            );

            return;
        }

        if (responseOptions == null || responseOptions.Length == 0)
        {
            Debug.LogWarning(
                "[GameManager] Radio message has no response options!",
                this
            );

            return;
        }

        if (requiredFrequency < 0 || requiredFrequency > 6)
        {
            Debug.LogWarning(
                "[GameManager] Required frequency must be between 0 and 6.",
                this
            );

            return;
        }

        // Always enqueue messages, even when the radio is OFF.
        RadioMessageData newMessage = new RadioMessageData(
            message,
            requiredFrequency,
            responseOptions
        );

        pendingRadioMessages.Enqueue(newMessage);

        if (enableDebugLogs)
        {
            Debug.Log(
                $"[GameManager] Radio message queued. " +
                $"Required frequency: {requiredFrequency}. " +
                $"Queue size: {pendingRadioMessages.Count}",
                this
            );
        }

        DisplayNextRadioMessage();
    }

    private void DisplayNextRadioMessage()
    {
        // Cannot display a message without power.
        if (!IsRadioOn)
        {
            UpdateRadioUI();
            return;
        }

        // Do not replace a message currently being handled.
        if (CurrentRadioMessage != null)
        {
            UpdateRadioUI();
            return;
        }

        // Nothing waiting in the queue.
        if (pendingRadioMessages.Count == 0)
        {
            UpdateRadioUI();
            return;
        }

        RadioMessageData nextMessage =
            pendingRadioMessages.Dequeue();

        CurrentRadioMessage = nextMessage.Message;
        RequiredRadioFrequency = nextMessage.RequiredFrequency;

        CurrentRadioResponseOptions =
            (string[])nextMessage.ResponseOptions.Clone();

        SelectedRadioResponseIndex = -1;
        radioResponseAlreadySubmitted = false;

        // Generate distorted versions of the message and its responses.
        RegenerateDisplayedRadioText();

        UpdateRadioUI();

        RadioMessageReceived?.Invoke(
            CurrentRadioMessage,
            (string[])CurrentRadioResponseOptions.Clone()
        );

        if (enableDebugLogs)
        {
            Debug.Log(
                $"[GameManager] Displaying radio message. " +
                $"Required frequency: {RequiredRadioFrequency}.",
                this
            );
        }
    }

    public void ClearRadioMessage()
    {
        CurrentRadioMessage = null;
        RequiredRadioFrequency = 0;

        displayedRadioMessage = "";
        displayedRadioResponseOptions = Array.Empty<string>();

        CurrentRadioResponseOptions = Array.Empty<string>();
        SelectedRadioResponseIndex = -1;

        radioResponseAlreadySubmitted = false;

        RadioResponseSelectionChanged?.Invoke(-1);

        DisplayNextRadioMessage();

        if (enableDebugLogs)
        {
            Debug.Log("[GameManager] Current radio message cleared.", this);
        }
    }

    // =========================================================
    // RADIO RESPONSE SELECTION
    // =========================================================

    public void SelectRadioResponse(int optionIndex)
    {
        if (!IsRadioOn || CurrentRadioMessage == null)
            return;

        if (radioResponseAlreadySubmitted)
            return;

        if (optionIndex < 0 ||
            optionIndex >= CurrentRadioResponseOptions.Length)
        {
            Debug.LogWarning(
                "[GameManager] Invalid radio response option index!",
                this
            );

            return;
        }

        SelectedRadioResponseIndex = optionIndex;

        UpdateRadioUI();

        RadioResponseSelectionChanged?.Invoke(optionIndex);

        if (enableDebugLogs)
        {
            Debug.Log(
                $"[GameManager] Selected response: {SelectedRadioResponse}",
                this
            );
        }
    }

    // =========================================================
    // RADIO RESPONSE SUBMISSION
    // =========================================================

    public void SubmitRadioResponse()
    {
        if (!IsRadioOn)
            return;

        if (CurrentRadioMessage == null)
        {
            Debug.LogWarning(
                "[GameManager] No radio message to respond to!",
                this
            );

            return;
        }

        if (radioResponseAlreadySubmitted)
            return;

        // Require the correct frequency before transmitting.
        if (!IsRadioFrequencyCorrect)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning(
                    "[GameManager] Cannot transmit: incorrect radio frequency!",
                    this
                );
            }

            return;
        }

        if (SelectedRadioResponse == null)
        {
            Debug.LogWarning(
                "[GameManager] No radio response selected!",
                this
            );

            return;
        }

        radioResponseAlreadySubmitted = true;

        string response = SelectedRadioResponse;

        // Notify other gameplay systems.
        RadioResponseSubmitted?.Invoke(response);

        if (enableDebugLogs)
        {
            Debug.Log(
                $"[GameManager] Radio response sent: {response}",
                this
            );
        }

        // Finish the current message and display the next one.
        ClearRadioMessage();
    }

    // =========================================================
    // RADIO MESSAGE DISTORTION
    // =========================================================

    private void RegenerateDisplayedRadioText()
    {
        if (string.IsNullOrEmpty(CurrentRadioMessage))
        {
            displayedRadioMessage = "";
            displayedRadioResponseOptions = Array.Empty<string>();
            return;
        }

        // Generate the incoming message's distorted display text.
        displayedRadioMessage = GetDistortedMessage(CurrentRadioMessage);

        // Generate distorted display text for each response option.
        displayedRadioResponseOptions =
            new string[CurrentRadioResponseOptions.Length];

        for (int i = 0; i < CurrentRadioResponseOptions.Length; i++)
        {
            displayedRadioResponseOptions[i] =
                GetDistortedMessage(CurrentRadioResponseOptions[i]);
        }
    }

    private string GetDistortedMessage(string message)
    {
        int frequencyDifference = Mathf.Abs(
            CurrentRadioFrequency - RequiredRadioFrequency
        );

        if (frequencyDifference == 0)
            return message;

        float corruptionChance = Mathf.Clamp01(
            frequencyDifference * corruptionChancePerFrequencyStep
        );

        if (corruptionChance <= 0f)
            return message;

        char[] characters = message.ToCharArray();

        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";

        for (int i = 0; i < characters.Length; i++)
        {
            char original = characters[i];

            string replacementCharacters;

            if (char.IsUpper(original))
                replacementCharacters = uppercase;
            else if (char.IsLower(original))
                replacementCharacters = lowercase;
            else if (char.IsDigit(original))
                replacementCharacters = digits;
            else
                continue;

            // Random chance to corrupt this character.
            if (UnityEngine.Random.value >= corruptionChance)
                continue;

            char replacement;

            do
            {
                replacement = replacementCharacters[
                    UnityEngine.Random.Range(
                        0, replacementCharacters.Length
                    )
                ];
            }
            while (replacement == original);

            characters[i] = replacement;
        }

        return new string(characters);
    }

    // =========================================================
    // RADIO UI
    // =========================================================

    private void UpdateRadioUI()
    {
        // The radio interface should ALWAYS remain visible.
        if (radioMessagePanel != null &&
            !radioMessagePanel.activeSelf)
        {
            radioMessagePanel.SetActive(true);
        }

        bool hasMessage =
            !string.IsNullOrEmpty(CurrentRadioMessage);

        // -----------------------------------------------------
        // RADIO OFF
        // -----------------------------------------------------

        if (!IsRadioOn)
        {
            if (radioMessageText != null)
                radioMessageText.text = "TURN ON RADIO";

            if (radioFrequencyText != null)
                radioFrequencyText.text = "RADIO OFF";

            if (radioResponseOptionTexts != null)
            {
                foreach (TMP_Text optionText in radioResponseOptionTexts)
                {
                    if (optionText == null)
                        continue;

                    optionText.text = "TURN ON RADIO";
                    optionText.color = normalResponseColor;
                }
            }

            return;
        }

        // -----------------------------------------------------
        // RADIO ON: FREQUENCY
        // -----------------------------------------------------

        if (radioFrequencyText != null)
        {
            radioFrequencyText.text =
                $"FREQ {CurrentRadioFrequency:00}";
        }

        // -----------------------------------------------------
        // RADIO ON: MESSAGE
        // -----------------------------------------------------

        if (radioMessageText != null)
        {
            radioMessageText.text = hasMessage
                ? displayedRadioMessage
                : "AWAITING TRANSMISSION...";
        }

        // -----------------------------------------------------
        // RADIO ON: RESPONSE OPTIONS
        // -----------------------------------------------------

        if (radioResponseOptionTexts == null)
            return;

        for (int i = 0; i < radioResponseOptionTexts.Length; i++)
        {
            TMP_Text optionText = radioResponseOptionTexts[i];

            if (optionText == null)
                continue;

            bool optionExists =
                hasMessage &&
                i < CurrentRadioResponseOptions.Length;

            optionText.text =
                optionExists && i < displayedRadioResponseOptions.Length
                    ? displayedRadioResponseOptions[i]
                    : "";

            optionText.color =
                i == SelectedRadioResponseIndex && hasMessage
                    ? selectedResponseColor
                    : normalResponseColor;
        }
    }

    private void QueueMockRadioMessages()
    {
        // MESSAGE 1 — Required frequency: 5
        ReceiveRadioMessage(
            "This is fishing vessel 04. We are approaching the western channel. Visibility is deteriorating. Requesting navigation instructions.",
            5,
            new string[]
            {
                "Proceed through the western channel.",
                "Change course to the eastern channel.",
                "Remain outside the channel until visibility improves."
            }
        );

        // MESSAGE 2 — Required frequency: 1
        ReceiveRadioMessage(
            "Lighthouse keeper, this is cargo vessel 12. We have spotted unusual debris near the southern reef. Is the passage safe?",
            1,
            new string[]
            {
                "Continue at reduced speed.",
                "Turn around and use the northern route.",
                "Proceed carefully. The southern passage is clear."
            }
        );

        // MESSAGE 3 — Required frequency: 3
        ReceiveRadioMessage(
            "This is coastal patrol. We are requesting confirmation that the lighthouse beacon is operational.",
            3,
            new string[]
            {
                "Beacon is operational.",
                "Beacon is currently malfunctioning.",
                "Unable to confirm beacon status."
            }
        );

        if (enableDebugLogs)
        {
            Debug.Log(
                "[GameManager] Three mock radio transmissions queued.",
                this
            );
        }
    }
}
