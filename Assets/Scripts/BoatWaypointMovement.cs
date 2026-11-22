using UnityEngine;

public class BoatWaypointMovement : MonoBehaviour
{
    [Header("Route")]
    [SerializeField] private Transform[] waypoints;

    [Header("Movement")]
    [SerializeField] private float speed = 1.8f;
    [SerializeField] private float turnSpeed = 22f;
    [SerializeField] private float waypointReachDistance = 4f;

    [Header("Orientation")]
    [SerializeField] private Transform forwardReference;

    private int currentWaypointIndex = 1;
    private bool routeFinished = false;

    private void Start()
    {
        if (waypoints == null || waypoints.Length < 2)
        {
            Debug.LogError("[BoatMovement] You need at least 2 waypoints!");
            enabled = false;
            return;
        }

        foreach (Transform point in waypoints)
        {
            if (point == null)
            {
                Debug.LogError("[BoatMovement] One or more waypoints are missing!");
                enabled = false;
                return;
            }
        }

        if (forwardReference == null)
            forwardReference = transform;

        Debug.Log("[BoatMovement] Route started!");
    }

    private void Update()
    {
        if (routeFinished)
            return;

        Transform target = waypoints[currentWaypointIndex];

        // Direction towards the current waypoint.
        Vector3 direction = target.position - transform.position;

        // Ignore vertical differences; the floating script handles height.
        direction.y = 0f;

        // Check whether we have reached the waypoint.
        if (direction.magnitude <= waypointReachDistance)
        {
            currentWaypointIndex++;

            // Stop after the final waypoint.
            if (currentWaypointIndex >= waypoints.Length)
            {
                routeFinished = true;
                Debug.Log("[BoatMovement] Route completed!");
                return;
            }

            target = waypoints[currentWaypointIndex];

            direction = target.position - transform.position;
            direction.y = 0f;
        }

        if (direction.sqrMagnitude < 0.001f)
            return;

        // Calculate the direction in which the boat should face.
        Vector3 desiredForward = direction.normalized;

        Vector3 currentForward = GetForwardDirection();

        // Calculate how much we need to rotate around the Y axis.
        float angle = Vector3.SignedAngle(
            currentForward,
            desiredForward,
            Vector3.up
        );

        float turnStep = turnSpeed * Time.deltaTime;

        angle = Mathf.Clamp(angle, -turnStep, turnStep);

        // Rotate the boat root.
        transform.rotation =
            Quaternion.AngleAxis(angle, Vector3.up) * transform.rotation;

        // Move in the direction the bow is facing.
        transform.position +=
            GetForwardDirection() * speed * Time.deltaTime;
    }

    private Vector3 GetForwardDirection()
    {
        Vector3 forward = Vector3.ProjectOnPlane(
            forwardReference.forward,
            Vector3.up
        );

        return forward.normalized;
    }

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2)
            return;

        Gizmos.color = Color.cyan;

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null)
                continue;

            Gizmos.DrawSphere(waypoints[i].position, 0.5f);

            if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(
                    waypoints[i].position,
                    waypoints[i + 1].position
                );
            }
        }
    }
}