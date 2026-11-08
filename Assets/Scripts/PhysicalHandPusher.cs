
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PhysicalHandPusher : MonoBehaviour
{
    [SerializeField] private Transform trackingTarget;
    [SerializeField] private Vector3 localOffset;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (trackingTarget == null)
            return;

        Vector3 targetPosition =
            trackingTarget.TransformPoint(localOffset);

        Quaternion targetRotation = trackingTarget.rotation;

        rb.MovePosition(targetPosition);
        rb.MoveRotation(targetRotation);
    }
}
