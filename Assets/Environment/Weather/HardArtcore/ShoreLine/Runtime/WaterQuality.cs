using UnityEngine;

namespace HardArtcore.ShoreLine
{
    public enum QualityTier
    {
        Low = 0,
        Medium = 1,
        High = 2,
    }

    /// <summary>
    /// Quality tiers switch the costly features on or off. Artistic toggles (waves on/off, intersection foam,
    /// shore waves) are left as authored, so e.g. a pool without shore waves stays without them.
    /// Tiers are chosen per material in the Editor; unused features are stripped from builds. To change
    /// quality at runtime, swap between preset materials.
    /// </summary>
    public static class WaterQuality
    {
        /// <summary>Every feature keyword and the material Toggle property that mirrors it.</summary>
        public static readonly (string keyword, string toggle)[] KeywordToggles =
        {
            ("_HAW_WAVES", "_WavesEnabled"),
            ("_HAW_WAVE_DAMPING", "_WaveDampingEnabled"),
            ("_HAW_INTERSECTION_FOAM", "_IntersectionFoamEnabled"),
            ("_HAW_SURFACE_FOAM", "_SurfaceFoamEnabled"),
            ("_HAW_SHORE_WAVES", "_ShoreWavesEnabled"),
            ("_HAW_REFRACTION", "_RefractionEnabled"),
            ("_HAW_CAUSTICS", "_CausticsEnabled"),
            ("_HAW_RECEIVE_SHADOWS", "_ReceiveShadowsEnabled"),
            ("_HAW_REFLECTIONS", "_ReflectionsEnabled"),
        };

        // Tuned on device (docs/perf/2026-11-19-results.md): environment reflections were ~90% of Low's cost on a
        // throttled OnePlus 3T, so Low drops them and keeps intersection foam and shore waves.
        static (int maxWaves, bool refraction, bool caustics, bool surfaceFoam, bool shadows, bool reflections) Settings(QualityTier tier) => tier switch
        {
            QualityTier.Low => (2, false, false, false, false, false),
            QualityTier.Medium => (3, true, false, true, false, true),
            _ => (4, true, true, true, true, true),
        };

        public static void Apply(Material material, QualityTier tier)
        {
            if (material == null) return;
            var s = Settings(tier);
            SetFeature(material, "_HAW_REFRACTION", s.refraction);
            SetFeature(material, "_HAW_CAUSTICS", s.caustics);
            SetFeature(material, "_HAW_SURFACE_FOAM", s.surfaceFoam);
            SetFeature(material, "_HAW_RECEIVE_SHADOWS", s.shadows);
            SetFeature(material, "_HAW_REFLECTIONS", s.reflections);
            if (material.HasProperty("_WaveCount"))
                material.SetFloat("_WaveCount", Mathf.Min(material.GetFloat("_WaveCount"), s.maxWaves));
            if (material.HasProperty("_QualityTier"))
                material.SetFloat("_QualityTier", (float)tier);
        }

        /// <summary>Sets a feature keyword and its Toggle property together, so the inspector shows the truth.</summary>
        public static void SetFeature(Material material, string keyword, bool on)
        {
            if (on) material.EnableKeyword(keyword); else material.DisableKeyword(keyword);
            foreach (var (k, toggle) in KeywordToggles)
                if (k == keyword && material.HasProperty(toggle))
                    material.SetFloat(toggle, on ? 1f : 0f);
        }
    }
}
