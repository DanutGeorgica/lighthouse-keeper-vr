
using UnityEngine;

[DisallowMultipleComponent]
public class WaterPlaneBoatFollower : MonoBehaviour
{
    public enum WaveCoordinateSpace
    {
        WaterSurfaceLocal,
        World
    }

    [Header("References")]
    [SerializeField] private Transform waterSurface;
    [SerializeField] private Transform boatVisual;
    [SerializeField] private Material waterMaterial;

    [Header("Wave Coordinate Space")]
    [SerializeField]
    private WaveCoordinateSpace coordinateSpace =
        WaveCoordinateSpace.WaterSurfaceLocal;

    [Header("Wave Settings")]
    [SerializeField] private bool readSettingsFromMaterial = true;

    [SerializeField] private float swellHeight = 0.12f;
    [SerializeField] private float swellLength = 4.2f;
    [SerializeField] private float swellSpeed = 0.85f;

    [Header("Boat Following")]
    [SerializeField] private bool followWaveTilt = true;

    [Range(0f, 1f)]
    [SerializeField] private float tiltStrength = 0.65f;

    [SerializeField] private float sampleDistance = 0.5f;
    [SerializeField] private float waterlineOffset = 0f;

    [Header("Smoothing")]
    [SerializeField] private float positionSharpness = 8f;
    [SerializeField] private float rotationSharpness = 5f;

    private Vector3 initialVisualLocalPosition;
    private Quaternion initialVisualLocalRotation;

    private static readonly int SwellHeightID =
        Shader.PropertyToID("_Swell_Height");

    private static readonly int SwellLengthID =
        Shader.PropertyToID("_Swell_Length");

    private static readonly int SwellSpeedID =
        Shader.PropertyToID("_Swell_Speed");

    private void Awake()
    {
        if (boatVisual == null)
        {
            Debug.LogError(
                "WaterPlaneBoatFollower: Boat Visual is not assigned!",
                this
            );

            enabled = false;
            return;
        }

        // Remember the model's original position and rotation.
        initialVisualLocalPosition = boatVisual.localPosition;
        initialVisualLocalRotation = boatVisual.localRotation;
    }

    private void Start()
    {
        if (waterSurface == null)
        {
            Debug.LogError(
                "WaterPlaneBoatFollower: Water Surface is not assigned!",
                this
            );

            enabled = false;
            return;
        }

        LoadWaveSettings();
    }

    private void LoadWaveSettings()
    {
        if (!readSettingsFromMaterial || waterMaterial == null)
            return;

        if (!waterMaterial.HasProperty(SwellHeightID) ||
            !waterMaterial.HasProperty(SwellLengthID) ||
            !waterMaterial.HasProperty(SwellSpeedID))
        {
            Debug.LogWarning(
                "WaterPlaneBoatFollower: Could not find all swell properties. " +
                "Using manually configured values.",
                this
            );

            return;
        }

        swellHeight = waterMaterial.GetFloat(SwellHeightID);
        swellLength = waterMaterial.GetFloat(SwellLengthID);
        swellSpeed = waterMaterial.GetFloat(SwellSpeedID);

        Debug.Log(
            $"Water settings loaded: Height={swellHeight}, " +
            $"Length={swellLength}, Speed={swellSpeed}",
            this
        );
    }

    private void LateUpdate()
    {
        if (waterSurface == null || boatVisual == null)
            return;

        float time = Time.time;

        // Get the boat's position in the water's coordinate space.
        Vector3 wavePosition = WorldToWaveSpace(transform.position);

        // Calculate the centre water height.
        Vector3 baseWaterPoint = WaveToWorldSpace(wavePosition);

        Vector3 displacedWaterPoint =
            SampleDisplacedWorldPoint(wavePosition, time);

        Vector3 waveOffset = displacedWaterPoint - baseWaterPoint;

        // Keep the boat's horizontal position and movement
        // controlled by the parent object.
        Vector3 targetPosition =
            transform.TransformPoint(initialVisualLocalPosition)
            + waveOffset
            + WaterUp() * waterlineOffset;

        // Calculate the surface normal using four nearby samples.
        float distance = Mathf.Max(0.05f, sampleDistance);

        Vector3 left = SampleDisplacedWorldPoint(
            wavePosition + Vector3.left * distance, time);

        Vector3 right = SampleDisplacedWorldPoint(
            wavePosition + Vector3.right * distance, time);

        Vector3 back = SampleDisplacedWorldPoint(
            wavePosition + Vector3.back * distance, time);

        Vector3 front = SampleDisplacedWorldPoint(
            wavePosition + Vector3.forward * distance, time);

        Vector3 tangentX = right - left;
        Vector3 tangentZ = front - back;

        Vector3 normal = Vector3.Cross(tangentZ, tangentX).normalized;

        if (Vector3.Dot(normal, WaterUp()) < 0f)
            normal = -normal;

        if (normal.sqrMagnitude < 0.001f)
            normal = WaterUp();

        // Preserve the boat's original heading while tilting it
        // to match the water surface.
        Quaternion baseRotation =
            transform.rotation * initialVisualLocalRotation;

        Quaternion targetRotation = baseRotation;

        if (followWaveTilt)
        {
            Vector3 forward = Vector3.ProjectOnPlane(
                baseRotation * Vector3.forward,
                normal
            );

            if (forward.sqrMagnitude > 0.001f)
            {
                Quaternion tiltedRotation =
                    Quaternion.LookRotation(forward.normalized, normal);

                targetRotation = Quaternion.Slerp(
                    baseRotation,
                    tiltedRotation,
                    tiltStrength
                );
            }
        }

        // Smooth the vertical movement.
        float positionBlend =
            1f - Mathf.Exp(-positionSharpness * Time.deltaTime);

        boatVisual.position = Vector3.Lerp(
            boatVisual.position,
            targetPosition,
            positionBlend
        );

        // Smooth the pitch and roll.
        float rotationBlend =
            1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);

        boatVisual.rotation = Quaternion.Slerp(
            boatVisual.rotation,
            targetRotation,
            rotationBlend
        );
    }

    private float SampleSwellHeight(Vector3 position, float time)
    {
        float k = 6.2831853f / Mathf.Max(swellLength, 0.1f);

        Vector2 direction1 =
            new Vector2(1f, 0.35f).normalized;

        Vector2 direction2 =
            new Vector2(-0.43f, 1f).normalized;

        Vector2 horizontalPosition =
            new Vector2(position.x, position.z);

        float a =
            Vector2.Dot(horizontalPosition, direction1) * k
            - time * swellSpeed;

        float b =
            Vector2.Dot(horizontalPosition, direction2) * k * 1.63f
            - time * swellSpeed * 1.19f;

        return (Mathf.Sin(a) + Mathf.Sin(b) * 0.42f)
               * swellHeight;
    }

    private Vector3 SampleDisplacedWorldPoint(
        Vector3 position,
        float time)
    {
        float height = SampleSwellHeight(position, time);

        position.y += height;

        return WaveToWorldSpace(position);
    }

    private Vector3 WorldToWaveSpace(Vector3 position)
    {
        return coordinateSpace == WaveCoordinateSpace.WaterSurfaceLocal
            ? waterSurface.InverseTransformPoint(position)
            : position;
    }

    private Vector3 WaveToWorldSpace(Vector3 position)
    {
        return coordinateSpace == WaveCoordinateSpace.WaterSurfaceLocal
            ? waterSurface.TransformPoint(position)
            : position;
    }

    private Vector3 WaterUp()
    {
        return coordinateSpace == WaveCoordinateSpace.WaterSurfaceLocal
            ? waterSurface.up
            : Vector3.up;
    }

    private void OnValidate()
    {
        swellHeight = Mathf.Max(0f, swellHeight);
        swellLength = Mathf.Max(0.1f, swellLength);
        swellSpeed = Mathf.Max(0f, swellSpeed);

        sampleDistance = Mathf.Max(0.05f, sampleDistance);
        positionSharpness = Mathf.Max(0f, positionSharpness);
        rotationSharpness = Mathf.Max(0f, rotationSharpness);
    }
}
