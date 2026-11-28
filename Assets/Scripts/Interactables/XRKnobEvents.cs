
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Content.Interaction;

public class KnobEvents : MonoBehaviour
{
    [System.Serializable]
    public class FloatEvent : UnityEvent<float> { }

    [System.Serializable]
    public class IntEvent : UnityEvent<int> { }

    [Header("Reference")]
    [SerializeField] private XRKnob knob;

    [Header("Events")]
    [SerializeField] private FloatEvent onValueChanged;
    [SerializeField] private IntEvent onPositionChanged;

    [Header("Runtime Information")]
    [SerializeField] private float currentValue;
    [SerializeField] private int currentPosition;

    [Header("Position Configuration")]
    [SerializeField] private float angleIncrement = 30f;
    [SerializeField] private bool reversePositionOrder = false;

    private float previousValue;
    private int previousPosition;

    public float CurrentValue => currentValue;
    public int CurrentPosition => currentPosition;

    private void Awake()
    {
        if (knob == null)
            knob = GetComponent<XRKnob>();
    }

    private void Start()
    {
        if (knob == null)
        {
            Debug.LogError("KnobEvents: No XRKnob component found!", this);
            enabled = false;
            return;
        }

        // Read the initial state without triggering events.
        currentValue = knob.value;
        previousValue = currentValue;

        currentPosition = CalculatePosition(currentValue);
        previousPosition = currentPosition;
    }

    private void Update()
    {
        if (knob == null)
            return;

        currentValue = knob.value;

        // Detect an actual value change.
        if (!Mathf.Approximately(currentValue, previousValue))
        {
            previousValue = currentValue;

            onValueChanged?.Invoke(currentValue);

            int newPosition = CalculatePosition(currentValue);

            // Only trigger the position event when the
            // discrete position has actually changed.
            if (newPosition != previousPosition)
            {
                previousPosition = newPosition;
                currentPosition = newPosition;

                onPositionChanged?.Invoke(currentPosition);
            }
        }
    }

    private int CalculatePosition(float normalizedValue)
    {
        if (knob == null || angleIncrement <= 0f || knob.maxAngle <= knob.minAngle)
            return -1;

        float angleRange = knob.maxAngle - knob.minAngle;
        float angleFromMin = Mathf.Clamp01(normalizedValue) * angleRange;

        int positionFromMin = Mathf.RoundToInt(angleFromMin / angleIncrement);
        int totalSteps = Mathf.RoundToInt(angleRange / angleIncrement);

        if (reversePositionOrder)
            return totalSteps - positionFromMin;

        return positionFromMin;
    }

    private void OnValidate()
    {
        angleIncrement = Mathf.Max(0f, angleIncrement);
    }
}
