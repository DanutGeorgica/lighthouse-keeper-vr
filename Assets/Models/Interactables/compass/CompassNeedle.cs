
using UnityEngine;

public class CompassNeedle : MonoBehaviour
{
    [SerializeField] private Transform needlePivot;
    [SerializeField] private Transform needleAxis;
    [SerializeField] private Vector3 worldNorth = Vector3.forward;

    [Header("Spring Movement")]
    [SerializeField] private float springStrength = 4f;
    [SerializeField] private float damping = 3f;

    private float angularVelocity;

    private void LateUpdate()
    {
        // Get the actual rotation axis of the compass face.
        Vector3 rotationAxis = needleAxis.up;

        Vector3 northDirection =
            Vector3.ProjectOnPlane(worldNorth, rotationAxis).normalized;

        Vector3 currentDirection =
            Vector3.ProjectOnPlane(needlePivot.forward, rotationAxis).normalized;

        if (northDirection.sqrMagnitude < 0.001f ||
            currentDirection.sqrMagnitude < 0.001f)
            return;

        // Calculate the angular difference around the correct axis.
        float angleDifference = Vector3.SignedAngle(
            currentDirection,
            northDirection,
            rotationAxis
        );

        // Spring acceleration and damping.
        float angularAcceleration =
            angleDifference * springStrength -
            angularVelocity * damping;

        angularVelocity += angularAcceleration * Time.deltaTime;

        // Rotate exclusively around the compass face's normal.
        needlePivot.rotation =
            Quaternion.AngleAxis(
                angularVelocity * Time.deltaTime,
                rotationAxis
            ) * needlePivot.rotation;
    }
}
