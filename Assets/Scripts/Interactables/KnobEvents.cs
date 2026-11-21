
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
            Debug.LogError("KnobEvents: No XRKnob component found!",this);

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

                //Debug.Log($"{gameObject.name}: Knob position changed to {currentPosition}",this);
            }
        }
    }

    private int CalculatePosition(float value)
    {
        if (knob == null ||
            knob.maxAngle <= knob.minAngle ||
            knobAngleIncrement <= 0f)
        {
            return -1;
        }

        float angleRange = knob.maxAngle - knob.minAngle;

        float angle = Mathf.Clamp01(value) * angleRange;

        return Mathf.RoundToInt(angle / knobAngleIncrement);
    }

    private float knobAngleIncrement => angleIncrement;

    [Header("Position Configuration")]
    [SerializeField] private float angleIncrement = 30f;

    private void OnValidate()
    {
        angleIncrement = Mathf.Max(0f, angleIncrement);
    }
}
