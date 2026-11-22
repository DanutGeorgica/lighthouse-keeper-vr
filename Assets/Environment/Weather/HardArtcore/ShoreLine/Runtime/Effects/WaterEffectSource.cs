using UnityEngine;
using UnityEngine.Rendering;

namespace HardArtcore.ShoreLine
{
    /// <summary>
    /// Per-camera information for <see cref="WaterEffectSource.Draw"/>. A struct passed by reference, so new fields
    /// can be added in later versions without breaking existing add-ons.
    /// </summary>
    public readonly struct SurfaceEffectsContext
    {
        /// <summary>The camera the buffer is rendered for (game, scene view...).</summary>
        public Camera Camera { get; }

        /// <summary>World XZ area covered by the buffer this frame (x = min X, y = min Z). Skip draws outside it.</summary>
        public Rect WorldRect { get; }

        /// <summary>World size of one buffer texel in meters.</summary>
        public float TexelSize { get; }

        /// <summary>Buffer resolution in texels per side.</summary>
        public int Resolution { get; }

        public SurfaceEffectsContext(Camera camera, Rect worldRect, float texelSize, int resolution)
        {
            Camera = camera;
            WorldRect = worldRect;
            TexelSize = texelSize;
            Resolution = resolution;
        }
    }

    /// <summary>
    /// Base class for anything that draws into the Surface Effects buffer (ripples, wakes, rain...).
    /// Part of the add-on contract.
    /// <list type="bullet">
    /// <item>Draw with shaders built on HAW_SurfaceEffectsSource.hlsl (use HAW_WorldToEffectsClip for positions).</item>
    /// <item>Use the contract blend state <c>Blend One One</c> + <c>BlendOp Add, Max</c> and output
    /// (height m, -dh/dx, -dh/dz, foam 0..1): RGB is signed and summed across sources, A takes the max.</item>
    /// <item>Pass per-draw values with a MaterialPropertyBlock: material values are read when the command buffer
    /// executes, so sources sharing a material would overwrite each other.</item>
    /// <item>Add <c>[ExecuteAlways]</c> to your subclass to see effects in edit mode (the attribute isn't inherited).</item>
    /// <item>If you override OnEnable/OnDisable, call base.OnEnable()/base.OnDisable() or the source never registers.</item>
    /// </list>
    /// The buffer is rendered before opaques; your own render-graph passes can read it as the global texture
    /// _HAW_SurfaceEffects (declare it with UseGlobalTexture). Requires ShoreLineFeature on the URP renderer.
    /// </summary>
    public abstract class WaterEffectSource : MonoBehaviour
    {
        protected virtual void OnEnable() => WaterEffects.Register(this);

        protected virtual void OnDisable() => WaterEffects.Unregister(this);

        /// <summary>Called once per camera per frame while the Surface Effects buffer is the render target.</summary>
        public abstract void Draw(RasterCommandBuffer cmd, in SurfaceEffectsContext context);
    }
}
