using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace HardArtcore.ShoreLine
{
    /// <summary>
    /// Registry of Surface Effects sources. Part of the add-on contract. ShoreLineFeature renders them and sets
    /// _HAW_SurfaceEffectsEnabled to 1 for each camera it draws the buffer for (0 otherwise), so the water shader
    /// skips the effects entirely (uniform branch) when no add-on is active or the camera has no buffer.
    /// </summary>
    public static class WaterEffects
    {
        static readonly List<WaterEffectSource> sources = new List<WaterEffectSource>();
        static readonly int TextureId = Shader.PropertyToID("_HAW_SurfaceEffects");
        static bool? formatSupported;

        public static IReadOnlyList<WaterEffectSource> Sources => sources;

        /// <summary>The buffer needs a signed half-float target that supports blending (GLES3.2 / Adreno 530 do).</summary>
        public static bool FormatSupported
        {
            get
            {
                if (!formatSupported.HasValue)
                {
                    formatSupported = SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, GraphicsFormatUsage.Blend);
                    if (!formatSupported.Value)
                        Debug.LogWarning("[ShoreLine] This device can't blend into half-float render targets; water surface effects are disabled.");
                }
                return formatSupported.Value;
            }
        }

        public static void Register(WaterEffectSource source)
        {
            if (source == null || sources.Contains(source)) return;
            sources.Add(source);
        }

        public static void Unregister(WaterEffectSource source)
        {
            // Add-on shaders may sample the texture directly: leave them a valid one.
            if (sources.Remove(source) && sources.Count == 0) Shader.SetGlobalTexture(TextureId, Texture2D.blackTexture);
        }
    }
}
