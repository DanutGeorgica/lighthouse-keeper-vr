
using UnityEngine;

[DisallowMultipleComponent]
public class LighthouseBeacon : MonoBehaviour
{
    [Header("Beacon Lights")]
    [SerializeField] private Light[] beaconLights;

    [Header("Rotation")]
    [Tooltip("The Transform that should rotate. Defaults to this GameObject.")]
    [SerializeField] private Transform rotatingPart;

    [Tooltip("Local axis around which the beacon rotates.")]
    [SerializeField] private Vector3 rotationAxis = Vector3.up;

    [Tooltip("Original rotation speed in degrees per second.")]
    [SerializeField, Min(0f)] private float baseRotationSpeed = 15f;

    [Header("Rotation Speed Steps")]
    [Tooltip("Seven multipliers, from slowest to fastest.")]
    [SerializeField]
    private float[] rotationSpeedMultipliers =
    {
        0.5f,
        0.67f,
        0.83f,
        1f,
        1.17f,
        1.33f,
        1.5f
    };

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    public bool IsPowered { get; private set; }
    public int CurrentSpeedStep { get; private set; } = 3;

    private float currentSpeedMultiplier = 1f;

    private void Awake()
    {
        if (rotatingPart == null)
            rotatingPart = transform;

        if (rotationAxis.sqrMagnitude < 0.001f)
            rotationAxis = Vector3.up;

        rotationAxis.Normalize();
    }

    private void Update()
    {
        if (!IsPowered || rotatingPart == null)
            return;

        float rotationAmount =
            baseRotationSpeed *
            currentSpeedMultiplier *
            Time.deltaTime;

        rotatingPart.Rotate(
            rotationAxis,
            rotationAmount,
            Space.Self
        );
    }

    public void SetPowered(bool powered)
    {
        IsPowered = powered;

        foreach (Light beaconLight in beaconLights)
        {
            if (beaconLight != null)
                beaconLight.enabled = powered;
        }

        if (enableDebugLogs)
        {
            Debug.Log(
                $"[LighthouseBeacon] Beacon {(powered ? "ON" : "OFF")}.",
                this
            );
        }
    }

    public void SetRotationSpeedStep(int step)
    {
        if (rotationSpeedMultipliers == null ||
            rotationSpeedMultipliers.Length != 7)
        {
            Debug.LogError(
                "[LighthouseBeacon] Rotation Speed Multipliers must contain exactly 7 values!",
                this
            );
            return;
        }

        step = Mathf.Clamp(step, 0, 6);

        CurrentSpeedStep = step;
        currentSpeedMultiplier = rotationSpeedMultipliers[step];

        if (enableDebugLogs)
        {
            Debug.Log(
                $"[LighthouseBeacon] Rotation speed set to " +
                $"step {step + 1} ({currentSpeedMultiplier}x).",
                this
            );
        }
    }

    private void OnValidate()
    {
        if (rotationAxis.sqrMagnitude < 0.001f)
            rotationAxis = Vector3.up;

        if (rotationSpeedMultipliers == null ||
            rotationSpeedMultipliers.Length != 7)
        {
            rotationSpeedMultipliers = new float[]
            {
                0.5f, 0.67f, 0.83f, 1f,
                1.17f, 1.33f, 1.5f
            };
        }
    }
}
