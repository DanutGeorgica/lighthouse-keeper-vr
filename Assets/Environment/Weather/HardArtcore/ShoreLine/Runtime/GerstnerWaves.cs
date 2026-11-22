using System;
using UnityEngine;

namespace HardArtcore.ShoreLine
{
    /// <summary>
    /// CPU mirror of HAW_Waves.hlsl. Each wave is packed as (dirX, dirZ, steepness 0..1, wavelength m),
    /// exactly like the material properties _WaveA.._WaveD. Any change here must be mirrored in the shader.
    /// </summary>
    public static class GerstnerWaves
    {
        const float Gravity = 9.8f;
        const float MinWavelength = 0.01f;
        const float MinDirection = 1e-4f;
        const int InversionIterations = 8;
        const float MinJacobian = 0.2f;

        /// <summary>Displacement of the surface point that starts at <paramref name="positionWS"/> (XZ used).</summary>
        public static Vector3 Offset(Vector3 positionWS, ReadOnlySpan<Vector4> waves, float speed, float time)
        {
            var offset = Vector3.zero;
            float steepnessScale = SteepnessScale(waves);
            foreach (var w in waves)
            {
                var dir = new Vector2(w.x, w.y);
                float len = dir.magnitude;
                if (len < MinDirection) continue;
                dir /= len;

                float k = 2f * Mathf.PI / Mathf.Max(w.w, MinWavelength);
                float c = Mathf.Sqrt(Gravity / k) * speed;
                float a = Mathf.Clamp01(w.z) * steepnessScale / k;
                float f = k * (dir.x * positionWS.x + dir.y * positionWS.z - c * time);
                float cos = Mathf.Cos(f);

                offset.x += dir.x * a * cos;
                offset.y += a * Mathf.Sin(f);
                offset.z += dir.y * a * cos;
            }
            return offset;
        }

        /// <summary>
        /// World height of the displaced surface above world XZ of <paramref name="positionWS"/> (its Y is ignored).
        /// <paramref name="damping"/> scales the whole displacement (see <see cref="ShallowWaterDamping"/>).
        /// </summary>
        public static float SampleHeight(Vector3 positionWS, ReadOnlySpan<Vector4> waves, float speed, float time, float baseHeight,
            float damping = 1f)
        {
            // Gerstner moves points horizontally too, so find the source point whose displaced XZ lands on positionWS:
            // Newton's method on displaced(p) = target. Exact for mixed waves; within a few cm at the sharpest crests
            // (all steepness in one direction, total 1), where the surface has a cusp.
            var p = new Vector3(positionWS.x, 0f, positionWS.z);
            for (int i = 0; i < InversionIterations; i++)
            {
                HorizontalStep(p, waves, speed, time, out float ox, out float oz, out float jxx, out float jxz, out float jzz);
                float rx = positionWS.x - (p.x + ox * damping);
                float rz = positionWS.z - (p.z + oz * damping);
                // Jacobian (symmetric 2x2). Near a sharp crest it goes singular and plain Newton overshoots onto
                // another crest, so lift its smallest eigenvalue to MinJacobian.
                float a = 1f + jxx * damping, b = jxz * damping, c = 1f + jzz * damping;
                float smallest = (a + c) * 0.5f - Mathf.Sqrt((a - c) * (a - c) * 0.25f + b * b);
                float shift = Mathf.Max(0f, MinJacobian - smallest);
                a += shift;
                c += shift;
                float det = a * c - b * b;
                p.x += (c * rx - b * rz) / det;
                p.z += (a * rz - b * rx) / det;
            }
            return baseHeight + Offset(p, waves, speed, time).y * damping;
        }

        /// <summary>Horizontal displacement at <paramref name="p"/> and its derivative minus identity (xx, xz, zz).</summary>
        static void HorizontalStep(Vector3 p, ReadOnlySpan<Vector4> waves, float speed, float time,
            out float ox, out float oz, out float jxx, out float jxz, out float jzz)
        {
            ox = oz = jxx = jxz = jzz = 0f;
            float steepnessScale = SteepnessScale(waves);
            foreach (var w in waves)
            {
                var dir = new Vector2(w.x, w.y);
                float len = dir.magnitude;
                if (len < MinDirection) continue;
                dir /= len;

                float k = 2f * Mathf.PI / Mathf.Max(w.w, MinWavelength);
                float c = Mathf.Sqrt(Gravity / k) * speed;
                float steepness = Mathf.Clamp01(w.z) * steepnessScale; // = amplitude * k
                float f = k * (dir.x * p.x + dir.y * p.z - c * time);
                float along = steepness / k * Mathf.Cos(f);    // displacement along dir
                float dAlong = -steepness * Mathf.Sin(f);       // its derivative along dir

                ox += dir.x * along;
                oz += dir.y * along;
                jxx += dAlong * dir.x * dir.x;
                jxz += dAlong * dir.x * dir.y;
                jzz += dAlong * dir.y * dir.y;
            }
        }

        /// <summary>
        /// Shallow Water Damping: wave displacement multiplier for water <paramref name="waterDepth"/> meters deep.
        /// 0 at the shoreline, easing up to 1 at <paramref name="dampingDepth"/> and below. Mirror of HAW_ShallowWaterDamping.
        /// </summary>
        public static float ShallowWaterDamping(float waterDepth, float dampingDepth)
        {
            float t = Mathf.Clamp01(waterDepth / Mathf.Max(dampingDepth, 1e-3f));
            return t * t * (3f - 2f * t);
        }

        /// <summary>Largest possible displacement along any axis; used to pad renderer bounds.</summary>
        public static float MaxAmplitude(ReadOnlySpan<Vector4> waves)
        {
            float sum = 0f, steepnessScale = SteepnessScale(waves);
            foreach (var w in waves)
            {
                if (new Vector2(w.x, w.y).magnitude < MinDirection) continue;
                sum += Mathf.Clamp01(w.z) * steepnessScale / (2f * Mathf.PI / Mathf.Max(w.w, MinWavelength));
            }
            return sum;
        }

        /// <summary>
        /// When the clamped steepness of all valid waves adds up to more than 1, the surface folds over itself
        /// and the height query has no single answer. Scale every wave down so the total is exactly 1.
        /// </summary>
        static float SteepnessScale(ReadOnlySpan<Vector4> waves)
        {
            float total = 0f;
            foreach (var w in waves)
                if (new Vector2(w.x, w.y).magnitude >= MinDirection)
                    total += Mathf.Clamp01(w.z);
            return total > 1f ? 1f / total : 1f;
        }
    }
}
