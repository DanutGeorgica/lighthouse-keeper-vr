
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BinocularZoom : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private XRGrabInteractable grabInteractable;
    [SerializeField] private Camera xrCamera;
    [SerializeField] private Transform eyeAlignment;
    [SerializeField] private Transform opticalForward;

    [Header("Visuals")]
    [SerializeField] private Transform modelRoot;

    private Renderer[] modelRenderers;
    private bool[] originalRendererStates;

    [Header("Zoom")]
    [SerializeField, Range(1f, 4f)]
    private float magnification = 2f;

    [SerializeField]
    private float activationDistance = 0.16f;

    [SerializeField, Range(0f, 1f)]
    private float minimumFacingDot = 0.85f;

    [Header("Viewing Mask")]
    [SerializeField] private GameObject binocularMask;

    [Header("Zoom Transition")]
    [SerializeField] private float zoomSmoothTime = 0.3f;

    private float currentMagnification = 1f;
    private float zoomVelocity;

    private Matrix4x4 baseLeftProjection;
    private Matrix4x4 baseRightProjection;

    private bool zoomActive;
    private bool usingStereoProjection;
    private float originalFieldOfView;

    private void Awake()
    {
        if (grabInteractable == null)
            grabInteractable = GetComponent<XRGrabInteractable>();

        if (xrCamera == null)
            xrCamera = Camera.main;

        if (xrCamera != null)
            originalFieldOfView = xrCamera.fieldOfView;

        if (modelRoot == null)
            modelRoot = transform.Find("Model");

        if (modelRoot != null)
        {
            modelRenderers = modelRoot.GetComponentsInChildren<Renderer>(true);
            originalRendererStates = new bool[modelRenderers.Length];

            for (int i = 0; i < modelRenderers.Length; i++)
            {
                originalRendererStates[i] = modelRenderers[i].enabled;
            }
        }
        else
        {
            Debug.LogError("Binocular model root was not assigned!");
        }
    }

    private void Update()
    {
        bool shouldZoom =
            grabInteractable != null &&
            grabInteractable.isSelected &&
            IsAlignedWithPlayer();

        if (shouldZoom != zoomActive)
            SetZoom(shouldZoom);
    }

    private void SetModelVisible(bool visible)
    {
        if (modelRenderers == null)
            return;

        for (int i = 0; i < modelRenderers.Length; i++)
        {
            modelRenderers[i].enabled =
                visible && originalRendererStates[i];
        }
    }

    private bool IsAlignedWithPlayer()
    {
        if (xrCamera == null ||
            eyeAlignment == null ||
            opticalForward == null)
            return false;

        float distance = Vector3.Distance(
            eyeAlignment.position,
            xrCamera.transform.position
        );

        float facing = Vector3.Dot(
            opticalForward.forward,
            xrCamera.transform.forward
        );

        return distance <= activationDistance &&
               facing >= minimumFacingDot;
    }

    private void SetZoom(bool enable)
    {
        if (xrCamera == null)
            return;

        if (enable)
        {
            if (xrCamera.stereoEnabled)
            {
                // Restore the original XR projection before capturing it.
                xrCamera.ResetStereoProjectionMatrices();

                baseLeftProjection =
                    xrCamera.GetStereoProjectionMatrix(
                        Camera.StereoscopicEye.Left
                    );

                baseRightProjection =
                    xrCamera.GetStereoProjectionMatrix(
                        Camera.StereoscopicEye.Right
                    );

                usingStereoProjection = true;
            }
            else
            {
                originalFieldOfView = xrCamera.fieldOfView;
                usingStereoProjection = false;
            }

            zoomActive = true;
            zoomVelocity = 0f;

            SetModelVisible(false);

            if (binocularMask != null)
                binocularMask.SetActive(true);
        }
        else
        {
            // Restore visuals immediately.
            SetModelVisible(true);

            if (binocularMask != null)
                binocularMask.SetActive(false);

            // Keep the zoom transition smooth.
            zoomActive = false;
            zoomVelocity = 0f;
        }
    }

    private void LateUpdate()
    {
        if (xrCamera == null)
            return;

        if (!zoomActive &&
            !usingStereoProjection &&
            Mathf.Approximately(currentMagnification, 1f))
            return;

        // Determine the desired magnification.
        float targetMagnification =
            zoomActive ? magnification : 1f;

        // Smoothly move towards it.
        currentMagnification = Mathf.SmoothDamp(
            currentMagnification,
            targetMagnification,
            ref zoomVelocity,
            zoomSmoothTime
        );

        currentMagnification = Mathf.Clamp(
            currentMagnification,
            1f,
            magnification
        );

        if (usingStereoProjection)
        {
            // Apply zoom to the ORIGINAL projection matrices.
            Matrix4x4 zoomScale = Matrix4x4.Scale(
                new Vector3(
                    currentMagnification,
                    currentMagnification,
                    1f
                )
            );

            Matrix4x4 left = zoomScale * baseLeftProjection;
            Matrix4x4 right = zoomScale * baseRightProjection;

            xrCamera.SetStereoProjectionMatrix(
                Camera.StereoscopicEye.Left,
                left
            );

            xrCamera.SetStereoProjectionMatrix(
                Camera.StereoscopicEye.Right,
                right
            );
        }
        else
        {
            // Non-XR fallback: calculate the equivalent FOV.
            float halfFov =
                originalFieldOfView * 0.5f * Mathf.Deg2Rad;

            float currentFov = 2f * Mathf.Atan(
                Mathf.Tan(halfFov) / currentMagnification
            ) * Mathf.Rad2Deg;

            xrCamera.fieldOfView = currentFov;
        }

        // Once zoom-out is complete, restore everything.
        if (!zoomActive && currentMagnification <= 1.001f)
        {
            currentMagnification = 1f;
            zoomVelocity = 0f;

            if (usingStereoProjection)
                xrCamera.ResetStereoProjectionMatrices();
            else
                xrCamera.fieldOfView = originalFieldOfView;

            usingStereoProjection = false;
        }
    }

    private void OnDisable()
    {
        if (xrCamera != null)
        {
            if (usingStereoProjection)
                xrCamera.ResetStereoProjectionMatrices();
            else
                xrCamera.fieldOfView = originalFieldOfView;
        }

        zoomActive = false;
        usingStereoProjection = false;
        currentMagnification = 1f;
        zoomVelocity = 0f;

        if (binocularMask != null)
            binocularMask.SetActive(false);

        SetModelVisible(true);
    }
}
