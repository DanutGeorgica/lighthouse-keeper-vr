
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[DisallowMultipleComponent]
public class PullCordEvents : MonoBehaviour
{
    // =========================================================
    // EVENT TYPES
    // =========================================================

    [Serializable]
    public class PullCordEvent : UnityEvent { }

    [Serializable]
    public class PullDistanceEvent : UnityEvent<float> { }

    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private XRGrabInteractable pullInteractable;

    [SerializeField] private Transform cordOutlet;
    [SerializeField] private Transform cordHandleAttachment;

    [SerializeField] private LineRenderer cordLine;

    // =========================================================
    // PULL SETTINGS
    // =========================================================

    [Header("Pull Settings")]

    [Tooltip("Minimum extension required to register a pull.")]
    [SerializeField, Min(0f)]
    private float pullStartDistance = 0.02f;

    [Tooltip("Extension required to trigger the pull event.")]
    [SerializeField, Min(0.01f)]
    private float pullThreshold = 0.18f;

    [Tooltip("Extension below which the cord is considered returned.")]
    [SerializeField, Min(0f)]
    private float returnDistance = 0.04f;

    // =========================================================
    // EVENTS
    // =========================================================

    [Header("Events")]

    [SerializeField]
    private PullCordEvent onHandleGrabbed = new PullCordEvent();

    [SerializeField]
    private PullCordEvent onHandleReleased = new PullCordEvent();

    [SerializeField]
    private PullCordEvent onPullStarted = new PullCordEvent();

    [SerializeField]
    private PullDistanceEvent onPullDistanceChanged =
        new PullDistanceEvent();

    [SerializeField]
    private PullCordEvent onPullThresholdReached =
        new PullCordEvent();

    [SerializeField]
    private PullCordEvent onReturnedToRest =
        new PullCordEvent();

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // =========================================================
    // RUNTIME INFORMATION
    // =========================================================

    [Header("Runtime Information (Play Mode)")]

    [SerializeField] private bool isHandleGrabbed;

    [SerializeField] private float currentPullDistance;

    [SerializeField] private float restingCordLength;

    [SerializeField] private bool pullInProgress;

    [SerializeField] private bool thresholdReached;

    private bool wasSelected;
    private bool initialized;

    public float CurrentPullDistance => currentPullDistance;

    public bool IsPullInProgress => pullInProgress;

    public bool HasReachedPullThreshold => thresholdReached;

    // =========================================================
    // INITIALIZATION
    // =========================================================

    private void Awake()
    {
        if (pullInteractable == null)
            pullInteractable = GetComponent<XRGrabInteractable>();
    }

    private void Start()
    {
        if (pullInteractable == null ||
            cordOutlet == null ||
            cordHandleAttachment == null)
        {
            Debug.LogError(
                "[PullCordEvents] Missing required references!",
                this
            );

            enabled = false;
            return;
        }

        // Store the initial cord distance.
        restingCordLength = Vector3.Distance(
            cordOutlet.position,
            cordHandleAttachment.position
        );

        // Configure the visual cord.
        if (cordLine != null)
        {
            cordLine.useWorldSpace = true;
            cordLine.positionCount = 2;

            UpdateCordVisual();
        }

        initialized = true;

        LogDebug(
            $"Initialized. Resting cord length: {restingCordLength:F3} m."
        );
    }

    // =========================================================
    // PULL DETECTION
    // =========================================================

    private void FixedUpdate()
    {
        if (!initialized)
            return;

        bool isSelected = pullInteractable.isSelected;

        isHandleGrabbed = isSelected;

        // Handle grabbed.
        if (isSelected && !wasSelected)
        {
            LogDebug("Handle grabbed.");

            onHandleGrabbed.Invoke();
        }

        // Handle released.
        if (!isSelected && wasSelected)
        {
            LogDebug("Handle released.");

            onHandleReleased.Invoke();
        }

        // Calculate the current cord extension.
        float currentCordLength = Vector3.Distance(
            cordOutlet.position,
            cordHandleAttachment.position
        );

        currentPullDistance = Mathf.Max(
            0f,
            currentCordLength - restingCordLength
        );

        // Detect the beginning of a pull.
        if (isSelected &&
            !pullInProgress &&
            currentPullDistance >= pullStartDistance)
        {
            pullInProgress = true;

            LogDebug(
                $"Pull started. Distance: {currentPullDistance:F3} m."
            );

            onPullStarted.Invoke();
        }

        // Process an ongoing pull.
        if (pullInProgress)
        {
            onPullDistanceChanged.Invoke(currentPullDistance);

            // Trigger only once per pull.
            if (isSelected &&
                !thresholdReached &&
                currentPullDistance >= pullThreshold)
            {
                thresholdReached = true;

                LogDebug(
                    $"Pull threshold reached! Distance: {currentPullDistance:F3} m."
                );

                onPullThresholdReached.Invoke();
            }

            // Detect when the handle returns to its resting position.
            if (currentPullDistance <= returnDistance)
            {
                pullInProgress = false;
                thresholdReached = false;

                LogDebug("Handle returned to rest.");

                onReturnedToRest.Invoke();
            }
        }

        wasSelected = isSelected;
    }

    // =========================================================
    // CORD VISUAL
    // =========================================================

    private void LateUpdate()
    {
        UpdateCordVisual();
    }

    private void UpdateCordVisual()
    {
        if (cordLine == null ||
            cordOutlet == null ||
            cordHandleAttachment == null)
        {
            return;
        }

        cordLine.SetPosition(0, cordOutlet.position);
        cordLine.SetPosition(1, cordHandleAttachment.position);
    }

    // =========================================================
    // DEBUG LOGGING
    // =========================================================

    private void LogDebug(string message)
    {
        if (!enableDebugLogs)
            return;

        Debug.Log($"[{gameObject.name}] {message}", this);
    }

    // =========================================================
    // INSPECTOR VALIDATION
    // =========================================================

    private void OnValidate()
    {
        pullStartDistance = Mathf.Max(0f, pullStartDistance);

        pullThreshold = Mathf.Max(
            pullStartDistance + 0.001f,
            pullThreshold
        );

        returnDistance = Mathf.Clamp(
            returnDistance,
            0f,
            Mathf.Max(0f, pullThreshold - 0.001f)
        );
    }
}
