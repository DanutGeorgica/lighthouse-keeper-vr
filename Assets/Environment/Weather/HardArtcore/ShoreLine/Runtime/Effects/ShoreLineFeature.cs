using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HardArtcore.ShoreLine
{
    /// <summary>
    /// Add this to your URP renderer. It makes sure the camera depth (and, when needed, opaque) textures exist for
    /// the water, and renders the Surface Effects buffer that add-ons (ripples, wakes...) draw into.
    /// </summary>
    public class ShoreLineFeature : ScriptableRendererFeature
    {
        public const int MinResolution = 64;
        public const int MaxResolution = 2048;

        [Serializable]
        public class Settings
        {
            [Tooltip("Resolution of the Surface Effects buffer (pixels per side).")]
            [Range(MinResolution, MaxResolution)] public int bufferResolution = 512;

            [Tooltip("World size covered by the Surface Effects buffer, centered where the camera looks (meters).")]
            [Min(1f)] public float bufferWorldSize = 64f;

            [Tooltip("Request the camera opaque texture for Refraction. It is only requested while an active " +
                     "WaterSurface uses Refraction (or when no WaterSurface exists). Turn off if no water refracts.")]
            public bool requestOpaqueTexture = true;
        }

        public Settings settings = new Settings();

        static readonly int OpaqueAvailableId = Shader.PropertyToID("_HAW_OpaqueTextureAvailable");
        static readonly int EffectsEnabledId = Shader.PropertyToID("_HAW_SurfaceEffectsEnabled");

        SurfaceEffectsPass effectsPass;
        WaterInputsPass inputsPass;

        // Every camera starts without the opaque texture and the effects buffer; only this feature's passes, recorded
        // for that camera, switch them on. So a camera whose renderer lacks the feature (or has it disabled) falls
        // back safely instead of reading what another camera produced.
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void HookCameraReset()
        {
            RenderPipelineManager.beginCameraRendering -= ResetCameraGlobals;
            RenderPipelineManager.beginCameraRendering += ResetCameraGlobals;
        }

        static void ResetCameraGlobals(ScriptableRenderContext context, Camera camera)
        {
            Shader.SetGlobalFloat(OpaqueAvailableId, 0f);
            Shader.SetGlobalFloat(EffectsEnabledId, 0f);
        }

        public override void Create()
        {
            // Before opaques: anything later in the frame (water, add-on passes) can read the buffer.
            effectsPass = new SurfaceEffectsPass { renderPassEvent = RenderPassEvent.BeforeRenderingOpaques };
            // Inputs are requested from a pass before transparents, so URP copies depth/color after opaques
            // (cheap) instead of adding a depth prepass (expensive on mobile).
            inputsPass = new WaterInputsPass { renderPassEvent = RenderPassEvent.BeforeRenderingTransparents };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Preview) return;
            effectsPass.Setup(settings);
            inputsPass.Setup(settings.requestOpaqueTexture && AnyActiveWaterRefracts());
            renderer.EnqueuePass(effectsPass);
            renderer.EnqueuePass(inputsPass);
        }

        /// <summary>Requests the camera depth (always) and opaque (when water refracts) textures; records no work.</summary>
        class WaterInputsPass : ScriptableRenderPass
        {
            bool requestColor;

            public void Setup(bool color)
            {
                requestColor = color;
                ConfigureInput(ScriptableRenderPassInput.Depth |
                               (color ? ScriptableRenderPassInput.Color : ScriptableRenderPassInput.None));
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                // Per camera: lets the water fall back to alpha blending when there is no opaque texture.
                // Set at record time; URP records and executes each camera before the next one.
                bool opaqueAvailable = frameData.Get<UniversalCameraData>().requiresOpaqueTexture || requestColor;
                Shader.SetGlobalFloat(OpaqueAvailableId, opaqueAvailable ? 1f : 0f);
            }
        }

        /// <summary>
        /// True if any active WaterSurface uses Refraction. With no WaterSurface at all the water is unknown,
        /// so the opaque texture is still requested (add WaterSurface to water objects to save the copy on mobile).
        /// </summary>
        static bool AnyActiveWaterRefracts()
        {
            var surfaces = WaterSurface.Active;
            if (surfaces.Count == 0) return true;
            foreach (var surface in surfaces)
            {
                var m = surface.Material;
                if (m != null && m.IsKeywordEnabled("_HAW_REFRACTION")) return true;
            }
            return false;
        }

        /// <summary>
        /// Center of the buffer: where the camera's view ray meets the water plane (so orthographic and isometric
        /// cameras far from the water are covered). Perspective cameras stay within a quarter of the buffer size
        /// of their own position, so the water around them stays covered too.
        /// </summary>
        internal static Vector2 BufferCenter(Camera camera, float size)
        {
            var t = camera.transform;
            var pos = t.position;
            var forward = t.forward;
            var center = new Vector2(pos.x, pos.z);
            float waterY = WaterSurface.Active.Count > 0 ? WaterSurface.Active[0].transform.position.y : 0f;
            if (forward.y < -1e-3f)
            {
                float distance = (waterY - pos.y) / forward.y;
                if (distance > 0f)
                {
                    var offset = new Vector2(forward.x, forward.z) * distance;
                    if (!camera.orthographic) offset = Vector2.ClampMagnitude(offset, size * 0.25f);
                    center += offset;
                }
            }
            return center;
        }

        class SurfaceEffectsPass : ScriptableRenderPass
        {
            static readonly int TextureId = Shader.PropertyToID("_HAW_SurfaceEffects");
            static readonly int RectId = Shader.PropertyToID("_HAW_SurfaceEffectsRect");

            Settings settings;

            class PassData
            {
                public Vector4 rect;
                public SurfaceEffectsContext context;
                public readonly List<WaterEffectSource> sources = new List<WaterEffectSource>();
            }

            public void Setup(Settings s) => settings = s;

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var cameraData = frameData.Get<UniversalCameraData>();
                var sources = WaterEffects.Sources;
                if (sources.Count == 0 || !WaterEffects.FormatSupported) return;
                // Per camera, like the opaque flag: this camera gets a buffer, so the water may read it.
                Shader.SetGlobalFloat(EffectsEnabledId, 1f);

                int resolution = Mathf.Clamp(settings.bufferResolution, MinResolution, MaxResolution);
                float size = Mathf.Max(settings.bufferWorldSize, 1f);
                float texel = size / resolution;
                var center = BufferCenter(cameraData.camera, size);
                // Snap to whole texels so the buffer doesn't shimmer as the camera moves.
                float minX = Mathf.Floor(center.x / texel) * texel - size * 0.5f;
                float minZ = Mathf.Floor(center.y / texel) * texel - size * 0.5f;

                var desc = new TextureDesc(resolution, resolution)
                {
                    format = GraphicsFormat.R16G16B16A16_SFloat, // signed: height and normal offsets go negative
                    clearBuffer = true,
                    clearColor = Color.clear,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "_HAW_SurfaceEffects",
                };
                var texture = renderGraph.CreateTexture(desc);

                using (var builder = renderGraph.AddRasterRenderPass<PassData>("HAW Surface Effects", out var passData))
                {
                    passData.rect = new Vector4(minX, minZ, 1f / size, size);
                    passData.context = new SurfaceEffectsContext(cameraData.camera, new Rect(minX, minZ, size, size), texel, resolution);
                    passData.sources.Clear();
                    passData.sources.AddRange(sources);

                    builder.SetRenderAttachment(texture, 0, AccessFlags.Write);
                    // Also keeps the pass from being culled: its only consumer reads the global texture.
                    builder.AllowGlobalStateModification(true);
                    builder.SetGlobalTextureAfterPass(texture, TextureId);
                    builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                    {
                        context.cmd.SetGlobalVector(RectId, data.rect);
                        foreach (var source in data.sources)
                        {
                            if (source == null) continue;
                            try { source.Draw(context.cmd, data.context); }
                            catch (Exception e) { Debug.LogException(e, source); } // one broken add-on must not break the rest
                        }
                    });
                }
            }
        }
    }
}
