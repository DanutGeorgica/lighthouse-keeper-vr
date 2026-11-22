using System;
using System.Collections.Generic;
using UnityEngine;

namespace HardArtcore.ShoreLine
{
    /// <summary>
    /// Put this on every water object. It answers "how high is the water here?" with the same Gerstner math the
    /// shader uses, keeps the renderer bounds large enough for the waves, and in Ocean Mode makes the water mesh
    /// follow the camera. Add-ons find water through <see cref="Active"/>.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [AddComponentMenu("HardArtcore/ShoreLine/Water Surface")]
    public class WaterSurface : MonoBehaviour
    {
        static readonly List<WaterSurface> active = new List<WaterSurface>();

        /// <summary>All enabled water surfaces.</summary>
        public static IReadOnlyList<WaterSurface> Active => active;

        [Tooltip("Moves the water mesh with the camera (snapped to the grid step, so waves don't slide). " +
                 "Use with an ocean mesh from Tools > HardArtcore > ShoreLine > Create Water Mesh.")]
        [SerializeField] bool oceanMode;

        [Tooltip("Camera to follow in Ocean Mode. Empty = Camera.main.")]
        [SerializeField] Camera followCamera;

        readonly Vector4[] waves = new Vector4[4];
        int waveCount;
        float waveSpeed;
        float maxAmplitude;
        // ponytail: fixed guess at ripple/wake height; let sources report their max if add-ons go taller.
        const float SurfaceEffectsAllowance = 1f; // meters, up and down
        float dampingDepth; // 0 = Shallow Water Damping off

        public bool OceanMode { get => oceanMode; set => oceanMode = value; }
        public Camera FollowCamera { get => followCamera; set => followCamera = value; }

        public Material Material => TryGetComponent<MeshRenderer>(out var r) ? r.sharedMaterial : null;

        /// <summary>Spacing of the inner grid, read from the mesh (WaterMeshBuilder layout). 0 if unknown.</summary>
        public float GridStep { get; private set; }

        /// <summary>Half-size of the inner grid, read from the mesh (WaterMeshBuilder layout). 0 if unknown.</summary>
        public float InnerHalfSize { get; private set; }

        /// <summary>
        /// Ocean Mode only: true when wave displacement fades out inside the fine inner grid. The long ring quads
        /// can't carry wave shape, and the snapped camera can sit up to half a grid step off-center.
        /// </summary>
        public bool WaveFadeFitsGrid
        {
            get
            {
                var m = Material;
                if (!oceanMode || m == null || !m.HasProperty("_WaveFadeEnd")) return true;
                return m.GetFloat("_WaveFadeEnd") <= InnerHalfSize - GridStep * 0.5f;
            }
        }

        ReadOnlySpan<Vector4> Waves => new ReadOnlySpan<Vector4>(waves, 0, waveCount);

        void OnEnable()
        {
            if (!active.Contains(this)) active.Add(this);
            RefreshWaveCache();
        }

        void OnDisable() => active.Remove(this);

        void OnValidate() => RefreshWaveCache();

        /// <summary>
        /// Re-reads wave settings and the mesh layout. Called automatically on enable and inspector edits;
        /// call it yourself after changing the water material from code at runtime.
        /// </summary>
        public void RefreshWaveCache()
        {
            var m = Material;
            waveCount = 0;
            waveSpeed = 0f;
            dampingDepth = 0f;
            if (m != null && m.IsKeywordEnabled("_HAW_WAVES"))
            {
                waveCount = Mathf.Clamp((int)m.GetFloat("_WaveCount"), 0, 4);
                waves[0] = m.GetVector("_WaveA");
                waves[1] = m.GetVector("_WaveB");
                waves[2] = m.GetVector("_WaveC");
                waves[3] = m.GetVector("_WaveD");
                waveSpeed = m.GetFloat("_WaveSpeed");
                if (m.IsKeywordEnabled("_HAW_WAVE_DAMPING") && m.HasProperty("_WaveDampingDepth"))
                    dampingDepth = Mathf.Max(m.GetFloat("_WaveDampingDepth"), 1e-3f);
            }
            maxAmplitude = GerstnerWaves.MaxAmplitude(Waves);
            ReadMeshLayout();
            UpdateBounds();
        }

        /// <summary>
        /// World height of the water surface above <paramref name="positionWS"/> (its Y is ignored). Matches the
        /// shader's waves; ignores the distance fade, Shallow Water Damping (see the overload with water depth) and
        /// add-on surface effects (GPU only).
        /// </summary>
        public float SampleHeight(Vector3 positionWS) =>
            GerstnerWaves.SampleHeight(positionWS, Waves, waveSpeed, ShaderTime, transform.position.y);

        /// <summary>
        /// Like <see cref="SampleHeight(Vector3)"/>, plus Shallow Water Damping when the material uses it: pass the water depth
        /// (m) at this point, e.g. from a raycast down to your terrain. The GPU reads depth from the camera depth texture,
        /// which the CPU can't see, so <see cref="SampleHeight(Vector3)"/> alone returns undamped waves.
        /// </summary>
        public float SampleHeight(Vector3 positionWS, float waterDepth) =>
            GerstnerWaves.SampleHeight(positionWS, Waves, waveSpeed, ShaderTime, transform.position.y,
                dampingDepth > 0f ? GerstnerWaves.ShallowWaterDamping(waterDepth, dampingDepth) : 1f);

        /// <summary>The clock URP feeds into the shader's _Time.y (Time.time in play mode and players,
        /// Time.realtimeSinceStartup in edit mode). CPU wave queries must use this to stay in phase.</summary>
        public static float ShaderTime
        {
            get
            {
#if UNITY_EDITOR
                return Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
#else
                return Time.time;
#endif
            }
        }

        /// <summary>Camera position snapped to the grid step on XZ, at height <paramref name="y"/>.</summary>
        public static Vector3 SnapToGrid(Vector3 cameraPos, float spacing, float y) =>
            new Vector3(Mathf.Round(cameraPos.x / spacing) * spacing, y, Mathf.Round(cameraPos.z / spacing) * spacing);

        void LateUpdate()
        {
            // Only in play mode: moving the transform in edit mode would dirty the scene every frame.
            if (!oceanMode || !Application.isPlaying || GridStep <= 0f) return;
            var cam = followCamera != null ? followCamera : Camera.main;
            if (cam == null) return;
            transform.position = SnapToGrid(cam.transform.position, GridStep, transform.position.y);
        }

#if UNITY_EDITOR
        // Editor only: pick up material/mesh edits made outside this component's inspector (cheap: 4 vectors).
        void Update()
        {
            if (!Application.isPlaying) RefreshWaveCache();
        }
#endif

        Mesh layoutMesh;
        float localGridStep, localInnerHalfSize;

        void ReadMeshLayout()
        {
            var mesh = TryGetComponent<MeshFilter>(out var f) ? f.sharedMesh : null;
            if (mesh != layoutMesh)
            {
                layoutMesh = mesh;
                localGridStep = localInnerHalfSize = 0f;
                if (mesh != null && mesh.vertexCount >= 2 && mesh.isReadable)
                {
                    // WaterMeshBuilder layout: vertex 0 is the inner grid's -x/-z corner, vertex 1 the next one along +x.
                    var v = mesh.vertices;
                    localGridStep = Mathf.Max(0f, v[1].x - v[0].x);
                    localInnerHalfSize = Mathf.Max(0f, -v[0].x);
                }
            }
            // World units: snapping and the wave-fade check happen in world space.
            float scale = Mathf.Abs(transform.lossyScale.x);
            GridStep = localGridStep * scale;
            InnerHalfSize = localInnerHalfSize * scale;
        }

        void UpdateBounds()
        {
            if (!TryGetComponent<MeshFilter>(out var f) || f.sharedMesh == null || !TryGetComponent<MeshRenderer>(out var r))
                return;
            var bounds = f.sharedMesh.bounds;
            var scale = transform.lossyScale;
            // Waves are in world units; renderer bounds are local, so undo the object's scale.
            // Surface Effects (ripples, wakes) add height on the GPU too: keep a fixed vertical allowance for them.
            bounds.Expand(new Vector3(
                2f * maxAmplitude / Mathf.Max(Mathf.Abs(scale.x), 1e-4f),
                2f * (maxAmplitude + SurfaceEffectsAllowance) / Mathf.Max(Mathf.Abs(scale.y), 1e-4f),
                2f * maxAmplitude / Mathf.Max(Mathf.Abs(scale.z), 1e-4f)));
            r.localBounds = bounds;
        }
    }
}
