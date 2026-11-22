using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HardArtcore.ShoreLine.Demo
{
    /// <summary>
    /// Demo-only on-screen controls: quality tier (swaps the ocean material, the supported way to change quality at
    /// runtime), viewpoints, fps, and Hide UI for clean screenshots. IMGUI, so it needs no EventSystem or input module.
    /// </summary>
    [AddComponentMenu("HardArtcore/ShoreLine/Demo/Demo UI")]
    public class DemoUI : MonoBehaviour
    {
        [SerializeField] DemoCameraController cameraController;
        [SerializeField] WaterSurface ocean;
        [Tooltip("Ocean material per quality tier: Low, Medium, High.")]
        [SerializeField] Material[] tierMaterials = new Material[3];

        public DemoCameraController CameraController { get => cameraController; set => cameraController = value; }
        public WaterSurface Ocean { get => ocean; set => ocean = value; }
        public Material[] TierMaterials { get => tierMaterials; set => tierMaterials = value; }

        static readonly string[] TierNames = { "Low", "Medium", "High" };
        static readonly List<Rect> screenRects = new List<Rect>(); // screen space, origin bottom-left

        /// <summary>True if <paramref name="screenPosition"/> (pixels, origin bottom-left) is on a demo control.</summary>
        public static bool IsOverUI(Vector2 screenPosition)
        {
            foreach (var r in screenRects)
                if (r.Contains(screenPosition)) return true;
            return false;
        }

        int tier;
        bool hidden;
        float smoothedDt = 1f / 60f;
        GUIStyle label, button, selected, hint;
        readonly List<Texture2D> styleTextures = new List<Texture2D>();

        void OnDestroy()
        {
            foreach (var t in styleTextures) Destroy(t);
            styleTextures.Clear();
            button = null; // rebuilt by InitStyles if this component is used again
        }

        void Start()
        {
            Application.targetFrameRate = 60; // Android and iOS default to 30
            // Phones start at Medium (OnePlus 3T class hardware averages ~37 fps there); the buttons switch on the fly.
            SetTier(Application.isMobilePlatform ? 1 : 2);
        }

        void OnDisable() => screenRects.Clear();

        void SetTier(int i)
        {
            tier = i;
            ocean.GetComponent<MeshRenderer>().sharedMaterial = tierMaterials[i];
            ocean.RefreshWaveCache();
        }

        void Update()
        {
            smoothedDt = Mathf.Lerp(smoothedDt, Time.unscaledDeltaTime, 0.05f);
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame) hidden = !hidden;
#else
            if (Input.GetKeyDown(KeyCode.H)) hidden = !hidden;
#endif
        }

        void OnGUI()
        {
            if (Event.current.type == EventType.Layout) screenRects.Clear();
            // Scale a 720-unit-tall virtual layout to the screen's short side, so buttons stay finger-sized.
            float scale = Mathf.Min(Screen.width, Screen.height) / 720f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale, h = Screen.height / scale;
            InitStyles();

            const float pad = 16f, bh = 56f;
            if (hidden)
            {
                // Invisible corner button brings the UI back (and H on a keyboard).
                if (Button(new Rect(w - 120f, 0f, 120f, 120f), "", GUIStyle.none, scale)) hidden = false;
                return;
            }

            string stats = $"{1f / smoothedDt:F0} fps  ·  {TierNames[tier]}";
            GUI.Label(Track(pad, pad, Width(label, stats), 44f, scale), stats, label);
            if (Button(new Rect(w - pad - 140f, pad, 140f, bh), "Hide UI", button, scale)) hidden = true;

            // Bottom-left: quality tiers. Bottom-right: viewpoints (wraps upward if the screen is narrow).
            float x = pad, y = h - pad - bh;
            GUI.Label(new Rect(x, y - 40f, Width(hint, "Quality"), 32f), "Quality", hint);
            for (int i = 0; i < TierNames.Length; i++, x += 136f)
                if (Button(new Rect(x, y, 128f, bh), TierNames[i], i == tier ? selected : button, scale)) SetTier(i);

            var views = cameraController.Views;
            float vx = w - pad;
            float vy = y;
            for (int i = views.Length - 1; i >= 0; i--)
            {
                float bw = 24f + button.CalcSize(new GUIContent(views[i].name)).x;
                float left = vy == y ? pad + 3 * 136f + 16f : pad; // the first row shares space with the tier buttons
                if (vx - bw < left && vx < w - pad) { vx = w - pad; vy -= bh + 8f; }
                vx -= bw;
                if (Button(new Rect(vx, vy, bw, bh), views[i].name, button, scale)) cameraController.GoTo(i);
                vx -= 8f;
            }

#if ENABLE_INPUT_SYSTEM
            bool touch = Touchscreen.current != null;
#else
            bool touch = Input.touchSupported;
#endif
            string gestures = touch
                ? "Drag: orbit   ·   Pinch: zoom   ·   Two fingers: pan"
                : "Left drag: orbit   ·   Right drag: pan   ·   Scroll: zoom   ·   H: hide UI";
            GUI.Label(new Rect(pad, pad + 52f, Width(hint, gestures), 32f), gestures, hint);
        }

        // CalcSize runs a little short on device under the scaled GUI matrix; the slack keeps text inside its backing.
        static float Width(GUIStyle style, string text) => style.CalcSize(new GUIContent(text)).x + 16f;

        // Draws a button and records its screen rect so the camera ignores touches that start on it.
        static bool Button(Rect r, string text, GUIStyle style, float scale)
        {
            Track(r.x, r.y, r.width, r.height, scale);
            return GUI.Button(r, text, style);
        }

        static Rect Track(float x, float y, float width, float height, float scale)
        {
            var r = new Rect(x, y, width, height);
            if (Event.current.type == EventType.Layout)
                screenRects.Add(new Rect(x * scale, Screen.height - (y + height) * scale, width * scale, height * scale));
            return r;
        }

        void InitStyles()
        {
            if (button != null) return;
            Texture2D Fill(Color c)
            {
                var t = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
                t.SetPixel(0, 0, c);
                t.Apply();
                styleTextures.Add(t);
                return t;
            }
            var backing = Fill(new Color(0.03f, 0.12f, 0.18f, 0.55f));
            label = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, padding = new RectOffset(10, 10, 4, 4), clipping = TextClipping.Overflow, wordWrap = false,
                normal = { background = backing, textColor = Color.white } };
            hint = new GUIStyle(label) { fontSize = 20, fontStyle = FontStyle.Normal, normal = { textColor = new Color(1f, 1f, 1f, 0.85f) } };
            button = new GUIStyle(GUI.skin.button)
            {
                fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, border = new RectOffset(0, 0, 0, 0),
                normal = { background = Fill(new Color(0.05f, 0.2f, 0.3f, 0.7f)), textColor = Color.white },
                hover = { background = Fill(new Color(0.08f, 0.3f, 0.42f, 0.8f)), textColor = Color.white },
                active = { background = Fill(new Color(0.1f, 0.55f, 0.65f, 0.9f)), textColor = Color.white },
            };
            selected = new GUIStyle(button) { normal = { background = Fill(new Color(0.1f, 0.65f, 0.75f, 0.95f)), textColor = Color.white } };
            selected.hover = selected.normal;
        }
    }
}
