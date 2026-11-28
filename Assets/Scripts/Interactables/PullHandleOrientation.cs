
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class PullHandleOrientation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody handleBody;
    [SerializeField] private Transform cordOutlet;
    [SerializeField] private Transform cordHandleAttachment;

    [Tooltip("The handle's local up direction.")]
    [SerializeField] private Vector3 localUpDirection = Vector3.up;

    [Tooltip("The desired up direction in world space.")]
    [SerializeField] private Vector3 worldUpDirection = Vector3.up;

    [Header("Handle Orientation")]

    [Tooltip("Maximum rotational speed in degrees per second.")]
    [SerializeField, Min(1f)]
    private float rotationSpeed = 900f;

    // Direction from the handle pivot towards its cord attachment,
    // expressed in the handle's local space.
    private Vector3 localHandleToCordDirection;

    private bool initialized;

    private void Awake()
    {
        if (handleBody == null)
            handleBody = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (handleBody == null ||
            cordOutlet == null ||
            cordHandleAttachment == null)
        {
            Debug.LogError(
                "[PullHandleOrientation] Missing required references!",
                this
            );

            enabled = false;
            return;
        }

        // Calculate the direction from the handle pivot to its
        // cord attachment in local coordinates.
        localHandleToCordDirection =
            handleBody.transform.InverseTransformPoint(
                cordHandleAttachment.position
            ).normalized;

        if (localHandleToCordDirection.sqrMagnitude < 0.0001f)
        {
            Debug.LogError(
                "[PullHandleOrientation] Cord attachment cannot be at the handle pivot!",
                this
            );

            enabled = false;
            return;
        }

        initialized = true;
    }

    private void FixedUpdate()
    {
        if (!initialized)
            return;

        // Direction from the handle pivot towards the fixed outlet.
        Vector3 worldCordDirection =
            cordOutlet.position - handleBody.position;

        if (worldCordDirection.sqrMagnitude < 0.0001f)
            return;

        worldCordDirection.Normalize();

        // Define the handle's local up direction.
        Vector3 localUp = Vector3.ProjectOnPlane(
            localUpDirection.normalized,
            localHandleToCordDirection
        ).normalized;

        if (localUp.sqrMagnitude < 0.0001f)
        {
            localUp = Vector3.ProjectOnPlane(
                Vector3.forward,
                localHandleToCordDirection
            ).normalized;
        }

        // Define world up using the stationary reference.
        Vector3 worldUp = worldUpDirection.normalized;

        worldUp = Vector3.ProjectOnPlane(
            worldUp,
            worldCordDirection
        ).normalized;

        if (worldUp.sqrMagnitude < 0.0001f)
        {
            worldUp = Vector3.ProjectOnPlane(
                Vector3.forward,
                worldCordDirection
            ).normalized;
        }

        // Build the local and world orientation frames.
        Quaternion localFrame = Quaternion.LookRotation(
            localHandleToCordDirection,
            localUp
        );

        Quaternion worldFrame = Quaternion.LookRotation(
            worldCordDirection,
            worldUp
        );

        // Calculate the desired Rigidbody rotation.
        Quaternion targetRotation =
            worldFrame * Quaternion.Inverse(localFrame);

        // Smoothly rotate the physical object.
        Quaternion newRotation = Quaternion.RotateTowards(
            handleBody.rotation,
            targetRotation,
            rotationSpeed * Time.fixedDeltaTime
        );

        handleBody.MoveRotation(newRotation);
    }
}
