
using UnityEngine;

public class FresnelLensRotation : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotationsPerMinute = 1f;

    [SerializeField] private Vector3 localRotationAxis = Vector3.up;

    [Header("Initial State")]
    [SerializeField] private bool rotateOnStart = true;

    [Header("Runtime Information")]
    [SerializeField] private bool isRotating;

    public bool IsRotating => isRotating;

    private void Start()
    {
        isRotating = rotateOnStart;
    }

    private void Update()
    {
        if (!isRotating)
            return;

        // Convert RPM into degrees per second.
        float degreesPerSecond = rotationsPerMinute * 6f;

        // Rotate around the selected local axis.
        transform.Rotate(
            localRotationAxis.normalized,
            degreesPerSecond * Time.deltaTime,
            Space.Self
        );
    }

    public void StartRotation()
    {
        isRotating = true;
    }

    public void StopRotation()
    {
        isRotating = false;
    }

    public void SetRotation(bool shouldRotate)
    {
        isRotating = shouldRotate;
    }

    public void SetRotationSpeed(float newSpeed)
    {
        rotationsPerMinute = Mathf.Max(0f, newSpeed);
    }

    private void OnValidate()
    {
        rotationsPerMinute = Mathf.Max(0f, rotationsPerMinute);

        if (localRotationAxis.sqrMagnitude < 0.001f)
            localRotationAxis = Vector3.up;
    }
}
