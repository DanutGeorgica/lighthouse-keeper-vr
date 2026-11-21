
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRSimpleInteractable))]
public class PushButtonEvents : MonoBehaviour
{
    [Header("Button Events")]
    [SerializeField] private UnityEvent onPressed;
    [SerializeField] private UnityEvent onReleased;

    private XRSimpleInteractable interactable;

    private bool isPressed = false;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
    }

    private void OnEnable()
    {
        interactable = GetComponent<XRSimpleInteractable>();

        interactable.firstSelectEntered.AddListener(HandlePressed);

        interactable.lastSelectExited.AddListener(HandleReleased);
    }

    private void OnDisable()
    {
        if (interactable == null)
            return;

        interactable.firstSelectEntered.RemoveListener(HandlePressed);
        interactable.lastSelectExited.RemoveListener(HandleReleased);
    }

    private void HandlePressed(SelectEnterEventArgs args)
    {
        if (isPressed)
            return;

        isPressed = true;

        //Debug.Log($"{gameObject.name}: Button Pressed!");

        onPressed?.Invoke();
    }

    private void HandleReleased(SelectExitEventArgs args)
    {
        if (!isPressed)
            return;

        isPressed = false;

        //Debug.Log($"{gameObject.name}: Button Released!");

        onReleased?.Invoke();
    }
}
