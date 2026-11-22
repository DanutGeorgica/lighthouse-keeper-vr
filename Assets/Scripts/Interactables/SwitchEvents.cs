
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(HingeJoint))]
public class SwitchEvents : MonoBehaviour
{
    [System.Serializable]
    public class SwitchStateEvent : UnityEvent<bool> { }

    [Header("Hinge Joint")]
    [SerializeField] private HingeJoint hingeJoint;

    [Header("Switch Positions")]
    [Tooltip("Hinge angle when the switch is ON.")]
    [SerializeField] private float onAngle = 30f;

    [Tooltip("Hinge angle when the switch is OFF.")]
    [SerializeField] private float offAngle = -30f;

    [Tooltip("How close the switch must get to an endpoint to register its state.")]
    [SerializeField, Min(0f)] private float angleTolerance = 8f;

    [Header("Events")]
    [SerializeField] private UnityEvent onSwitchedOn;
    [SerializeField] private UnityEvent onSwitchedOff;
    [SerializeField] private SwitchStateEvent onStateChanged;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    [Header("Runtime Information (Play Mode)")]
    [SerializeField] private bool isOn;
    [SerializeField] private float currentHingeAngle;
    [SerializeField] private float distanceToOnAngle;
    [SerializeField] private float distanceToOffAngle;

    public bool IsOn => isOn;

    private void Awake()
    {
        if (hingeJoint == null)
            hingeJoint = GetComponent<HingeJoint>();
    }

    private void Start()
    {
        if (hingeJoint == null)
        {
            Debug.LogError(
                "[ToggleSwitchEvents] No HingeJoint found!",
                this
            );

            enabled = false;
            return;
        }

        UpdateAngle();

        // Determine the initial state from the closest endpoint.
        // Do not invoke events during initialization.
        isOn = distanceToOnAngle < distanceToOffAngle;
    }

    private void FixedUpdate()
    {
        if (hingeJoint == null)
            return;

        UpdateAngle();

        bool nearOnPosition = distanceToOnAngle <= angleTolerance;
        bool nearOffPosition = distanceToOffAngle <= angleTolerance;

        if (nearOnPosition && !nearOffPosition)
        {
            SetState(true);
        }
        else if (nearOffPosition && !nearOnPosition)
        {
            SetState(false);
        }
    }

    private void UpdateAngle()
    {
        currentHingeAngle = hingeJoint.angle;

        distanceToOnAngle = Mathf.Abs(
            Mathf.DeltaAngle(currentHingeAngle, onAngle)
        );

        distanceToOffAngle = Mathf.Abs(
            Mathf.DeltaAngle(currentHingeAngle, offAngle)
        );
    }

    private void SetState(bool newState)
    {
        if (isOn == newState)
            return;

        isOn = newState;

        if (enableDebugLogs)
        {
            Debug.Log(
                $"[{gameObject.name}] Switch turned {(isOn ? "ON" : "OFF")}!",
                this
            );
        }

        if (isOn)
            onSwitchedOn?.Invoke();
        else
            onSwitchedOff?.Invoke();

        onStateChanged?.Invoke(isOn);
    }
}
