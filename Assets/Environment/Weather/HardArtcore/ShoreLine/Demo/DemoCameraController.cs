using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HardArtcore.ShoreLine.Demo
{
    /// <summary>
    /// Demo-only orbit camera. Touch: one finger orbits, pinch zooms, two fingers pan.
    /// Mouse: left drag orbits, right/middle drag pans, scroll zooms. Works with either input system.
    /// </summary>
    [AddComponentMenu("HardArtcore/ShoreLine/Demo/Camera Controller")]
    public class DemoCameraController : MonoBehaviour
    {
        [Serializable]
        public struct View
        {
            public string name;
            public Vector3 pivot;
            public float yaw, pitch, distance;
        }

        [SerializeField] View[] views = { };
        [SerializeField] float minDistance = 4f;
        [SerializeField] float maxDistance = 150f;
        [Tooltip("Keeps the camera above the waves.")]
        [SerializeField] float minPitch = 8f;
        [SerializeField] float maxPitch = 85f;
        [Tooltip("How far (m) the orbit pivot may be panned from the scene origin.")]
        [SerializeField] float panRadius = 150f;
        [SerializeField] float smoothing = 10f;

        public View[] Views { get => views; set => views = value; }

        Vector3 pivot, smoothPivot;
        float yaw, pitch, distance, smoothYaw, smoothPitch, smoothDistance;
        readonly List<(Vector2 position, Vector2 delta)> touches = new List<(Vector2, Vector2)>();
        bool mouseBlocked;
#if !ENABLE_INPUT_SYSTEM
        readonly HashSet<int> blockedFingers = new HashSet<int>();
        Vector2 lastMousePosition;
#endif

        void Start()
        {
#if !ENABLE_INPUT_SYSTEM
            Input.simulateMouseWithTouches = false; // touches are handled as touches, not as a fake mouse
#endif
            if (views.Length > 0) GoTo(0, true);
        }

        public void GoTo(int index, bool instant = false)
        {
            var v = views[index];
            pivot = v.pivot;
            yaw = v.yaw;
            pitch = v.pitch;
            distance = v.distance;
            if (instant) { smoothPivot = pivot; smoothYaw = yaw; smoothPitch = pitch; smoothDistance = distance; }
        }

        void Update()
        {
            ReadTouches();
            if (touches.Count == 1)
                Orbit(touches[0].delta);
            else if (touches.Count >= 2)
            {
                var (a, da) = touches[0];
                var (b, db) = touches[1];
                float before = Vector2.Distance(a - da, b - db), now = Vector2.Distance(a, b);
                if (before > 1f && now > 1f) distance *= before / now;
                Pan((da + db) * 0.5f);
            }
            else
                ReadMouse();

            distance = Mathf.Clamp(distance, minDistance, maxDistance);
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            var flat = new Vector2(pivot.x, pivot.z);
            if (flat.magnitude > panRadius) { flat = flat.normalized * panRadius; pivot.x = flat.x; pivot.z = flat.y; }

            float t = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
            smoothPivot = Vector3.Lerp(smoothPivot, pivot, t);
            smoothYaw = Mathf.Lerp(smoothYaw, yaw, t);
            smoothPitch = Mathf.Lerp(smoothPitch, pitch, t);
            smoothDistance = Mathf.Lerp(smoothDistance, distance, t);
            var rotation = Quaternion.Euler(smoothPitch, smoothYaw, 0f);
            transform.SetPositionAndRotation(smoothPivot - rotation * Vector3.forward * smoothDistance, rotation);
        }

        // Pixel deltas are divided by screen height so a drag feels the same on every resolution.
        void Orbit(Vector2 delta)
        {
            yaw += delta.x / Screen.height * 180f;
            pitch -= delta.y / Screen.height * 120f;
        }

        void Pan(Vector2 delta)
        {
            var right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            var forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            pivot -= (right * delta.x + forward * delta.y) * (distance / Screen.height * 1.5f);
        }

        void Zoom(float steps) => distance *= Mathf.Pow(0.88f, steps);

        void ReadTouches()
        {
            touches.Clear();
#if ENABLE_INPUT_SYSTEM
            var screen = Touchscreen.current;
            if (screen == null) return;
            foreach (var t in screen.touches)
                if (t.isInProgress && !DemoUI.IsOverUI(t.startPosition.ReadValue()))
                    touches.Add((t.position.ReadValue(), t.delta.ReadValue()));
#else
            foreach (var t in Input.touches)
            {
                if (t.phase == TouchPhase.Began && DemoUI.IsOverUI(t.position)) blockedFingers.Add(t.fingerId);
                bool blocked = blockedFingers.Contains(t.fingerId);
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) blockedFingers.Remove(t.fingerId);
                else if (!blocked) touches.Add((t.position, t.deltaPosition));
            }
#endif
        }

        void ReadMouse()
        {
#if ENABLE_INPUT_SYSTEM
            var m = Mouse.current;
            if (m == null) return;
            bool orbit = m.leftButton.isPressed, pan = m.rightButton.isPressed || m.middleButton.isPressed;
            bool pressed = m.leftButton.wasPressedThisFrame || m.rightButton.wasPressedThisFrame || m.middleButton.wasPressedThisFrame;
            var position = m.position.ReadValue();
            var delta = m.delta.ReadValue();
            float scroll = m.scroll.ReadValue().y / 120f; // one wheel notch = 120
#else
            bool orbit = Input.GetMouseButton(0), pan = Input.GetMouseButton(1) || Input.GetMouseButton(2);
            bool pressed = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
            Vector2 position = Input.mousePosition;
            var delta = position - lastMousePosition;
            lastMousePosition = position;
            float scroll = Input.mouseScrollDelta.y;
#endif
            if (pressed) mouseBlocked = DemoUI.IsOverUI(position);
            if (!orbit && !pan) mouseBlocked = false;
            if (mouseBlocked) return;
            if (orbit) Orbit(delta);
            else if (pan) Pan(delta);
            if (!DemoUI.IsOverUI(position)) Zoom(scroll);
        }
    }
}
