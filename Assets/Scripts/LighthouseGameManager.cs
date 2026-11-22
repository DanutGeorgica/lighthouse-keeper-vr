
using System;
using UnityEngine;

[DisallowMultipleComponent]
public class LighthouseGameManager : MonoBehaviour
{
    public static LighthouseGameManager Instance { get; private set; }

    [Header("Beacon")]
    [SerializeField] private LighthouseBeacon lighthouseBeacon;
    [SerializeField] private bool beaconStartsOn = false;
    [SerializeField] private int beaconOnPosition = 1;

    [Header("Beacon Speed Knob")]
    [SerializeField, Range(0, 6)]
    private int beaconSpeedStartsAt = 3;

    [Header("Foghorn")]
    [SerializeField] private FoghornController foghornController;

    [Header("Interior Lights")]
    [SerializeField] private Light[] interiorLights;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // Current gameplay states.
    public bool IsBeaconOn { get; private set; }
    public int CurrentBeaconSpeedStep { get; private set; } = 3;
    public bool AreInteriorLightsOn { get; private set; }

    // Events for other systems.
    public event Action<bool> BeaconPowerChanged;
    public event Action<int> BeaconSpeedChanged;
    public event Action<bool> InteriorLightsChanged;
    public event Action FoghornTriggered;

    private void Awake()
    {
        // Singleton setup.
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
        // Apply initial beacon speed.
        ApplyBeaconSpeedStep(beaconSpeedStartsAt, false);

        // Force initial beacon state.
        ApplyBeaconState(beaconStartsOn, false);

        // Always force interior lights ON at startup.
        ApplyInteriorLights(true, false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
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

        IsBeaconOn = powered;

        if (lighthouseBeacon != null)
        {
            lighthouseBeacon.SetPowered(powered);
        }
        else
        {
            Debug.LogError(
                "[GameManager] LighthouseBeacon reference is missing!",
                this
            );
        }

        if (notify && stateChanged)
            BeaconPowerChanged?.Invoke(powered);

        if (enableDebugLogs)
        {
            Debug.Log(
                $"[GameManager] Beacon is now {(powered ? "ON" : "OFF")}.",
                this
            );
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

        AreInteriorLightsOn = powered;

        foreach (Light interiorLight in interiorLights)
        {
            if (interiorLight != null)
                interiorLight.enabled = powered;
        }

        if (notify && stateChanged)
            InteriorLightsChanged?.Invoke(powered);

        if (enableDebugLogs)
        {
            Debug.Log(
                $"[GameManager] Interior lights are now " +
                $"{(powered ? "ON" : "OFF")}.",
                this
            );
        }
    }
}
