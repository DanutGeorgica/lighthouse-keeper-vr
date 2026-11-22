
using UnityEngine;
using HardArtcore.ShoreLine;

[DisallowMultipleComponent]
public class WaterPlaneBoatFollower : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaterSurface water;
    [SerializeField] private Transform boatVisual;
    [SerializeField] private Transform forwardReference;

    [Header("Hull Sampling")]
    [SerializeField, Min(0.1f)]
    private float forwardSampleOffset = 3.2f;

    [SerializeField, Min(0.1f)]
    private float sideSampleOffset = 0.9f;

    [Header("Shallow Water")]
    [SerializeField] private bool useShallowWaterDamping;
    [SerializeField] private LayerMask groundMask;

    [Header("Boat Following")]
    [SerializeField] private bool followWaveTilt = true;

    [Range(0f, 1f)]
    [SerializeField] private float tiltStrength = 0.65f;

    [SerializeField] private float waterlineOffset = 0f;

    [Header("Smoothing")]
    [SerializeField, Min(0f)]
    private float positionSharpness = 8f;

    [SerializeField, Min(0f)]
    private float rotationSharpness = 5f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    [SerializeField, Min(0.25f)]
    private float debugInterval = 1f;

    private Transform drivenVisual;
    private bool visualIsRoot;

    private Vector3 initialVisualPositionInRoot;
    private Quaternion initialVisualRotationInRoot;
    private Vector3 initialForwardInRoot;

    private Vector3 initialRootLocalPosition;
    private Quaternion initialRootLocalRotation;
    private Vector3 initialForwardInParent;

    private float nextDebugTime;
    private float previousCenterHeight;
    private bool hasPreviousCenterHeight;

    private void Awake()
    {
        drivenVisual = boatVisual != null ? boatVisual : transform;
        visualIsRoot = drivenVisual == transform;

        Transform forwardSource =
            forwardReference != null ? forwardReference : drivenVisual;

        if (visualIsRoot)
        {
            initialRootLocalPosition = transform.localPosition;
            initialRootLocalRotation = transform.localRotation;

            if (transform.parent != null)
            {
                initialForwardInParent =
                    transform.parent.InverseTransformDirection(
                        forwardSource.forward
                    );
            }
            else
            {
                initialForwardInParent = forwardSource.forward;
            }

            initialForwardInParent.Normalize();
        }
        else
        {
            initialVisualPositionInRoot =
                transform.InverseTransformPoint(drivenVisual.position);

            initialVisualRotationInRoot =
                Quaternion.Inverse(transform.rotation) *
                drivenVisual.rotation;

            initialForwardInRoot =
                transform.InverseTransformDirection(
                    forwardSource.forward
                ).normalized;
        }

        if (enableDebugLogs)
        {
            Debug.Log(
                $"[BoatFollower] Awake on '{name}'\n" +
                $"Boat Root: {GetPath(transform)}\n" +
                $"Boat Visual: {GetPath(drivenVisual)}\n" +
                $"Visual Is Root: {visualIsRoot}\n" +
                $"Forward Reference: {GetPath(forwardReference)}\n" +
                $"Initial Visual Position: {drivenVisual.position}\n" +
                $"Initial Visual Rotation: {drivenVisual.eulerAngles}",
                this
            );
        }
    }

    private void Start()
    {
        if (water == null)
        {
            Debug.LogError(
                "[BoatFollower] ERROR: ShoreLine WaterSurface is NOT assigned!",
                this
            );

            enabled = false;
            return;
        }

        if (drivenVisual == null)
        {
            Debug.LogError(
                "[BoatFollower] ERROR: Boat Visual is missing!",
                this
            );

            enabled = false;
            return;
        }

        if (enableDebugLogs)
        {
            Renderer waterRenderer = water.GetComponent<Renderer>();
            MeshFilter waterMesh = water.GetComponent<MeshFilter>();

            string materialInfo = waterRenderer != null &&
                                  waterRenderer.sharedMaterial != null
                ? waterRenderer.sharedMaterial.name +
                  " | Shader: " +
                  waterRenderer.sharedMaterial.shader.name
                : "No Renderer/Material found on WaterSurface object";

            string meshInfo = waterMesh != null &&
                              waterMesh.sharedMesh != null
                ? waterMesh.sharedMesh.name +
                  " | Vertices: " +
                  waterMesh.sharedMesh.vertexCount
                : "No MeshFilter/Mesh found on WaterSurface object";

            Debug.Log(
                $"[BoatFollower] START CONFIGURATION\n" +
                $"Water: {GetPath(water.transform)}\n" +
                $"Water Enabled: {water.enabled}\n" +
                $"Water Active: {water.gameObject.activeInHierarchy}\n" +
                $"Water Position: {water.transform.position}\n" +
                $"Water Material: {materialInfo}\n" +
                $"Water Mesh: {meshInfo}\n" +
                $"Boat Visual: {GetPath(drivenVisual)}\n" +
                $"Boat Visual Active: {drivenVisual.gameObject.activeInHierarchy}\n" +
                $"Forward Sample: {forwardSampleOffset}\n" +
                $"Side Sample: {sideSampleOffset}\n" +
                $"Shallow Damping: {useShallowWaterDamping}\n" +
                $"Ground Mask: {groundMask.value}\n" +
                $"Follow Tilt: {followWaveTilt}\n" +
                $"Tilt Strength: {tiltStrength}\n" +
                $"Waterline Offset: {waterlineOffset}",
                this
            );
        }
    }

    private void LateUpdate()
    {
        if (water == null || drivenVisual == null)
            return;

        Vector3 basePosition;
        Quaternion baseRotation;
        Vector3 forward;

        if (visualIsRoot)
        {
            Transform parent = transform.parent;

            basePosition = parent != null
                ? parent.TransformPoint(initialRootLocalPosition)
                : initialRootLocalPosition;

            baseRotation = parent != null
                ? parent.rotation * initialRootLocalRotation
                : initialRootLocalRotation;

            forward = parent != null
                ? parent.TransformDirection(initialForwardInParent)
                : initialForwardInParent;
        }
        else
        {
            basePosition =
                transform.TransformPoint(initialVisualPositionInRoot);

            baseRotation =
                transform.rotation * initialVisualRotationInRoot;

            forward =
                transform.TransformDirection(initialForwardInRoot);
        }

        forward = Vector3.ProjectOnPlane(
            forward, Vector3.up
        ).normalized;

        if (forward.sqrMagnitude < 0.001f)
        {
            if (enableDebugLogs && Time.time >= nextDebugTime)
            {
                nextDebugTime = Time.time + debugInterval;

                Debug.LogWarning(
                    "[BoatFollower] Forward direction is invalid. " +
                    "Check the Boat Visual and Forward Reference rotations.",
                    this
                );
            }

            return;
        }

        Vector3 right = Vector3.Cross(
            Vector3.up, forward
        ).normalized;

        // Five sampling positions.
        Vector3 centerPoint = basePosition;

        Vector3 frontPoint =
            basePosition + forward * forwardSampleOffset;

        Vector3 backPoint =
            basePosition - forward * forwardSampleOffset;

        Vector3 leftPoint =
            basePosition - right * sideSampleOffset;

        Vector3 rightPoint =
            basePosition + right * sideSampleOffset;

        // Query ShoreLine.
        float centerHeight = GetWaterHeight(centerPoint);
        float frontHeight = GetWaterHeight(frontPoint);
        float backHeight = GetWaterHeight(backPoint);
        float leftHeight = GetWaterHeight(leftPoint);
        float rightHeight = GetWaterHeight(rightPoint);

        // Average sampled water height.
        float averageHeight =
            (centerHeight + frontHeight + backHeight +
             leftHeight + rightHeight) / 5f;

        Vector3 targetPosition = basePosition;
        targetPosition.y = averageHeight + waterlineOffset;

        // Calculate the surface normal.
        frontPoint.y = frontHeight;
        backPoint.y = backHeight;
        leftPoint.y = leftHeight;
        rightPoint.y = rightHeight;

        Vector3 tangentForward = frontPoint - backPoint;
        Vector3 tangentRight = rightPoint - leftPoint;

        Vector3 normal = Vector3.Cross(
            tangentForward, tangentRight
        ).normalized;

        if (normal.y < 0f)
            normal = -normal;

        if (normal.sqrMagnitude < 0.001f)
            normal = Vector3.up;

        // Calculate target rotation.
        Quaternion targetRotation = baseRotation;

        if (followWaveTilt)
        {
            Vector3 desiredUp = Vector3.Slerp(
                Vector3.up, normal, tiltStrength
            ).normalized;

            Quaternion tilt = Quaternion.FromToRotation(
                Vector3.up, desiredUp
            );

            targetRotation = tilt * baseRotation;
        }

        // Smooth position.
        float positionBlend = positionSharpness <= 0f
            ? 1f
            : 1f - Mathf.Exp(-positionSharpness * Time.deltaTime);

        Vector3 oldPosition = drivenVisual.position;

        drivenVisual.position = Vector3.Lerp(
            drivenVisual.position,
            targetPosition,
            positionBlend
        );

        // Smooth rotation.
        float rotationBlend = rotationSharpness <= 0f
            ? 1f
            : 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);

        Quaternion oldRotation = drivenVisual.rotation;

        drivenVisual.rotation = Quaternion.Slerp(
            drivenVisual.rotation,
            targetRotation,
            rotationBlend
        );

        // Periodic diagnostics.
        if (enableDebugLogs && Time.time >= nextDebugTime)
        {
            nextDebugTime = Time.time + debugInterval;

            float sampleMin = Mathf.Min(
                centerHeight, frontHeight, backHeight,
                leftHeight, rightHeight
            );

            float sampleMax = Mathf.Max(
                centerHeight, frontHeight, backHeight,
                leftHeight, rightHeight
            );

            string centerChange = hasPreviousCenterHeight
                ? (centerHeight - previousCenterHeight).ToString("+0.00000;-0.00000;0.00000")
                : "First sample";

            previousCenterHeight = centerHeight;
            hasPreviousCenterHeight = true;

            float positionError =
                Vector3.Distance(drivenVisual.position, targetPosition);

            float rotationError =
                Quaternion.Angle(drivenVisual.rotation, targetRotation);

            Debug.Log(
                $"[BoatFollower] FRAME DIAGNOSTICS\n" +

                $"Time: {Time.time:F2} | DeltaTime: {Time.deltaTime:F4}\n" +

                $"WATER HEIGHTS (world Y):\n" +
                $"Center: {centerHeight:F5}\n" +
                $"Bow: {frontHeight:F5}\n" +
                $"Stern: {backHeight:F5}\n" +
                $"Left: {leftHeight:F5}\n" +
                $"Right: {rightHeight:F5}\n" +
                $"Height Range: {(sampleMax - sampleMin):F5}\n" +
                $"Center Change Since Last Log: {centerChange}\n" +

                $"BOAT POSITION:\n" +
                $"Base Position: {basePosition}\n" +
                $"Current Visual Position: {drivenVisual.position}\n" +
                $"Target Position: {targetPosition}\n" +
                $"Position Error: {positionError:F5}\n" +
                $"Actual Movement This Frame: " +
                $"{Vector3.Distance(oldPosition, drivenVisual.position):F6}\n" +

                $"BOAT ROTATION:\n" +
                $"Current Rotation: {drivenVisual.eulerAngles}\n" +
                $"Target Rotation: {targetRotation.eulerAngles}\n" +
                $"Rotation Error: {rotationError:F4} degrees\n" +
                $"Normal: {normal}\n" +

                $"FOLLOW SETTINGS:\n" +
                $"Position Sharpness: {positionSharpness}\n" +
                $"Rotation Sharpness: {rotationSharpness}\n" +
                $"Follow Tilt: {followWaveTilt}\n" +
                $"Waterline Offset: {waterlineOffset}",
                this
            );
        }
    }

    private float GetWaterHeight(Vector3 position)
    {
        if (!useShallowWaterDamping || groundMask.value == 0)
            return water.SampleHeight(position);

        Ray ray = new Ray(
            position + Vector3.up * 50f,
            Vector3.down
        );

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            200f,
            groundMask,
            QueryTriggerInteraction.Ignore))
        {
            float waterDepth = Mathf.Max(
                0f,
                water.transform.position.y - hit.point.y
            );

            return water.SampleHeight(position, waterDepth);
        }

        return water.SampleHeight(position);
    }

    private static string GetPath(Transform target)
    {
        if (target == null)
            return "<not assigned>";

        string path = target.name;

        while (target.parent != null)
        {
            target = target.parent;
            path = target.name + "/" + path;
        }

        return path;
    }

    private void OnValidate()
    {
        forwardSampleOffset = Mathf.Max(0.1f, forwardSampleOffset);
        sideSampleOffset = Mathf.Max(0.1f, sideSampleOffset);
        positionSharpness = Mathf.Max(0f, positionSharpness);
        rotationSharpness = Mathf.Max(0f, rotationSharpness);
        debugInterval = Mathf.Max(0.25f, debugInterval);
    }
}
