
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(HingeJoint))]
public class LeverEvents : MonoBehaviour
{
    [System.Serializable]
    public class LeverStateEvent : UnityEvent<bool> { }

    [Header("Hinge Joint")]
    [SerializeField] private HingeJoint hingeJoint;

    [Header("ON/OFF Detection")]
    [Tooltip("Enable this if the lever moves towards positive angles when raised.")]
    [SerializeField] private bool onWhenAngleIsPositive = true;

    [Tooltip("Angle the lever must reach to turn ON.")]
    [SerializeField] private float turnOnAngle = 20f;

    [Tooltip("Angle the lever must return below to turn OFF.")]
    [SerializeField] private float turnOffAngle = 10f;

    [Header("Events")]
    [SerializeField] private LeverStateEvent onStateChanged;

    [Header("Runtime Information (Play Mode)")]
    [SerializeField] private bool isOn;
    [SerializeField] private float currentHingeAngle;
    [SerializeField] private float directedAngle;

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
            Debug.LogError("LeverEvents: No HingeJoint found!", this);
            enabled = false;
            return;
        }

        UpdateAngle();

        isOn = directedAngle >= turnOnAngle;
    }

    private void FixedUpdate()
    {
        if (hingeJoint == null)
            return;

        UpdateAngle();

        if (!isOn && directedAngle >= turnOnAngle)
        {
            SetState(true);
        }
        else if (isOn && directedAngle <= turnOffAngle)
        {
            SetState(false);
        }
    }

    private void UpdateAngle()
    {
        currentHingeAngle = hingeJoint.angle;

        directedAngle = onWhenAngleIsPositive
            ? currentHingeAngle
            : -currentHingeAngle;
    }

    private void SetState(bool newState)
    {
        if (isOn == newState)
            return;

        isOn = newState;

        //Debug.Log($"{gameObject.name}: Lever is {(isOn ? "ON" : "OFF")}!",this);

        onStateChanged?.Invoke(isOn);
    }
}
