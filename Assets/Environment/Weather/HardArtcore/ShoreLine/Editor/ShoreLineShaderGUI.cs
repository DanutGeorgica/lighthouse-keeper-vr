using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HardArtcore.ShoreLine.Editor
{
    /// <summary>Material inspector for HardArtcore/ShoreLine: sections, tier presets, setup check, links.</summary>
    public class ShoreLineShaderGUI : ShaderGUI
    {
        // (title, feature toggle property or null, properties shown when the feature is on)
        static readonly (string title, string toggle, string[] properties)[] Sections =
        {
            ("Color", null, new[] { "_ShallowColor", "_DeepColor", "_HorizonColor", "_DepthDistance", "_HorizonPower", "_EdgeFade" }),
            ("Waves", "_WavesEnabled", new[] { "_WaveCount", "_WaveA", "_WaveB", "_WaveC", "_WaveD", "_WaveSpeed", "_WaveFadeStart", "_WaveFadeEnd", "_WaveDampingEnabled", "_WaveDampingDepth" }),
            ("Normals & Lighting", null, new[] { "_NormalMap", "_NormalStrength", "_NormalTiling", "_NormalSpeed", "_SpecularColor", "_Smoothness" }),
            ("Foam", null, new[] { "_FoamTexture", "_FoamColor", "_FoamTiling", "_FoamSpeed" }),
            ("Intersection Foam", "_IntersectionFoamEnabled", new[] { "_IntersectionFoamWidth", "_IntersectionFoamSoftness" }),
            ("Surface Foam", "_SurfaceFoamEnabled", new[] { "_SurfaceFoamAmount", "_SurfaceFoamCrest" }),
            ("Shore Waves", "_ShoreWavesEnabled", new[] { "_ShoreWaveCount", "_ShoreWaveSpeed", "_ShoreWaveWidth", "_ShoreWaveRange" }),
            ("Refraction", "_RefractionEnabled", new[] { "_RefractionStrength" }),
            ("Caustics", "_CausticsEnabled", new[] { "_CausticsTexture", "_CausticsStrength", "_CausticsTiling", "_CausticsSpeed", "_CausticsDepth" }),
            ("Reflections", "_ReflectionsEnabled", new[] { "_ReflectionStrength" }),
            ("Shadows", "_ReceiveShadowsEnabled", new string[0]),
        };

        static readonly Dictionary<string, string> Tooltips = new Dictionary<string, string>
        {
            ["_ShallowColor"] = "Color (and opacity in A) where the water is shallow.",
            ["_DeepColor"] = "Color (and opacity in A) where the water is deep.",
            ["_HorizonColor"] = "Color seen at grazing angles. A = how strongly it is mixed in.",
            ["_DepthDistance"] = "Water depth in meters at which the Deep Color is fully reached.",
            ["_HorizonPower"] = "Higher = horizon color only very close to the horizon.",
            ["_EdgeFade"] = "Fades the water out where it meets objects, over this many meters of depth. 0 = off.",
            ["_WavesEnabled"] = "Gerstner waves that move the mesh. Needs a subdivided mesh (Tools > HardArtcore > ShoreLine > Create Water Mesh).",
            ["_WaveCount"] = "How many of the waves A-D are used. Fewer waves are cheaper.",
            ["_WaveA"] = "X, Y = direction on the XZ plane. Z = steepness 0-1. W = wavelength in meters.",
            ["_WaveB"] = "X, Y = direction on the XZ plane. Z = steepness 0-1. W = wavelength in meters.",
            ["_WaveC"] = "X, Y = direction on the XZ plane. Z = steepness 0-1. W = wavelength in meters.",
            ["_WaveD"] = "X, Y = direction on the XZ plane. Z = steepness 0-1. W = wavelength in meters.",
            ["_WaveSpeed"] = "Multiplier on the natural wave speed.",
            ["_WaveFadeStart"] = "Distance from the camera (m) where waves start flattening out.",
            ["_WaveFadeEnd"] = "Distance from the camera (m) where waves are completely flat. In Ocean Mode keep it inside the inner grid.",
            ["_WaveDampingEnabled"] = "Calms the waves in shallow water, using the camera depth texture, so shorelines on flat ground stay smooth instead of following the wave crests. Visual only: for buoyancy pass the water depth to WaterSurface.SampleHeight.",
            ["_WaveDampingDepth"] = "Water depth (m) at which waves reach their full height. Shallower water gets smaller waves, down to none at the shoreline.",
            ["_NormalMap"] = "Small ripples. Two scrolling copies are blended.",
            ["_NormalStrength"] = "Strength of the small ripples.",
            ["_NormalTiling"] = "Repeats per meter.",
            ["_NormalSpeed"] = "Scroll speed of the ripples.",
            ["_SpecularColor"] = "Color of the sun glint. Black = no glint.",
            ["_Smoothness"] = "Higher = smaller, sharper sun glint.",
            ["_FoamTexture"] = "Noise that breaks up all foam.",
            ["_FoamColor"] = "Foam color. A = foam strength.",
            ["_FoamTiling"] = "Foam noise repeats per meter.",
            ["_FoamSpeed"] = "Foam noise scroll speed.",
            ["_IntersectionFoamEnabled"] = "Foam line where objects and the shore touch the water.",
            ["_IntersectionFoamWidth"] = "Water depth (m) over which the foam line fades out.",
            ["_IntersectionFoamSoftness"] = "Softness of the foam edges.",
            ["_SurfaceFoamEnabled"] = "Foam patches on the open water, more on wave crests.",
            ["_SurfaceFoamAmount"] = "Foam coverage everywhere.",
            ["_SurfaceFoamCrest"] = "Extra foam on wave crests.",
            ["_ShoreWavesEnabled"] = "Foam bands rolling toward the shore. Uses water depth, so avoid on flat shallow floors (pools).",
            ["_ShoreWaveCount"] = "Number of bands.",
            ["_ShoreWaveSpeed"] = "How fast bands roll toward the shore.",
            ["_ShoreWaveWidth"] = "Band thickness.",
            ["_ShoreWaveRange"] = "Water depth (m) where the bands start.",
            ["_RefractionEnabled"] = "Distorts what is seen through the water. Needs the camera opaque texture, which the ShoreLine Feature requests for you. Off on Low tier.",
            ["_RefractionStrength"] = "Distortion amount.",
            ["_CausticsEnabled"] = "Animated light patterns on the ground under the water.",
            ["_CausticsTexture"] = "Caustics pattern.",
            ["_CausticsStrength"] = "Brightness of the caustics.",
            ["_CausticsTiling"] = "Repeats per meter.",
            ["_CausticsSpeed"] = "Animation speed.",
            ["_CausticsDepth"] = "Caustics fade out at this water depth (m).",
            ["_ReflectionsEnabled"] = "Reflects the sky / reflection probes at grazing angles. Off = horizon color only (cheaper).",
            ["_ReflectionStrength"] = "Reflection of the sky / reflection probes at grazing angles.",
            ["_ReceiveShadowsEnabled"] = "Shadows from objects fall onto the water surface.",
        };

        List<UnityEngine.Rendering.Universal.UniversalRendererData> missingRenderers;
        double nextSetupCheck;
        bool noUniversalRenderer, compatibilityMode;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            DrawHeader();
            DrawSetupCheck();
            DrawQualityTier(materialEditor, properties);
            EditorGUILayout.Space();

            foreach (var (title, toggle, names) in Sections)
                DrawSection(materialEditor, properties, title, toggle, names);

            EditorGUILayout.Space();
            materialEditor.RenderQueueField();
            materialEditor.EnableInstancingField();
        }

        static void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label($"HardArtcore ShoreLine {ShoreLineInfo.CoreVersion.ToString(3)}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("Docs", ShoreLineInfo.DocsUrl), EditorStyles.miniButtonLeft, GUILayout.Width(50)))
                    Application.OpenURL(ShoreLineInfo.DocsUrl);
                if (GUILayout.Button(new GUIContent("HardArtcore.com", ShoreLineInfo.WebsiteUrl), EditorStyles.miniButtonRight, GUILayout.Width(110)))
                    Application.OpenURL(ShoreLineInfo.WebsiteUrl);
            }
        }

        void DrawSetupCheck()
        {
            if (!(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline is UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset))
            {
                EditorGUILayout.HelpBox("ShoreLine needs the Universal Render Pipeline (URP) with a Universal Renderer. " +
                                        "Assign a URP asset in Project Settings > Graphics.", MessageType.Error);
                return;
            }

            // Re-check at most once a second; the inspector repaints often.
            if (missingRenderers == null || EditorApplication.timeSinceStartup > nextSetupCheck)
            {
                var renderers = ShoreLineSetup.ActiveRenderers().ToList();
                noUniversalRenderer = renderers.Count == 0;
                compatibilityMode = ShoreLineSetup.CompatibilityModeOn;
                missingRenderers = ShoreLineSetup.MissingFeature(renderers).ToList();
                nextSetupCheck = EditorApplication.timeSinceStartup + 1.0;
            }
            if (compatibilityMode)
            {
                EditorGUILayout.HelpBox("ShoreLine needs Render Graph. Turn off Compatibility Mode (Render Graph Disabled) " +
                                        "in Project Settings > Graphics > URP.", MessageType.Error);
                return;
            }
            if (noUniversalRenderer)
            {
                EditorGUILayout.HelpBox("No Universal Renderer found in your URP assets. ShoreLine needs a Universal Renderer; " +
                                        "the 2D Renderer isn't supported.", MessageType.Error);
                return;
            }
            if (missingRenderers.Count == 0) return;

            EditorGUILayout.HelpBox(
                $"ShoreLine Feature is missing or switched off on {string.Join(", ", missingRenderers.Select(r => r ? r.name : "(deleted)"))}. " +
                "Without it the water may get no depth texture (wrong colors, no foam) and add-on effects won't render.",
                MessageType.Warning);
            if (GUILayout.Button("Fix: add ShoreLine Feature"))
            {
                foreach (var r in missingRenderers) ShoreLineSetup.Install(r);
                missingRenderers = null;
            }
        }

        static void DrawQualityTier(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            var tierProp = FindProperty("_QualityTier", properties, false);
            if (tierProp == null) return;
            EditorGUI.showMixedValue = tierProp.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            var tier = (QualityTier)EditorGUILayout.EnumPopup(
                new GUIContent("Quality Tier", "Switches the costly features: Low = no refraction, caustics, surface foam or shadows, max 2 waves " +
                                               "(a lowered wave count stays lowered when you switch back up). " +
                                               "Your own on/off choices for waves, intersection foam and shore waves are kept."),
                (QualityTier)Mathf.RoundToInt(tierProp.floatValue));
            EditorGUI.showMixedValue = false;
            if (!EditorGUI.EndChangeCheck()) return;

            var materials = materialEditor.targets.OfType<Material>().ToArray();
            Undo.RecordObjects(materials, "Change Water Quality Tier");
            foreach (var m in materials)
            {
                WaterQuality.Apply(m, tier);
                EditorUtility.SetDirty(m);
            }
        }

        static void DrawSection(MaterialEditor materialEditor, MaterialProperty[] properties, string title, string toggle, string[] names)
        {
            string key = "HAW.ShoreLine.Section." + title;
            bool open = EditorGUILayout.BeginFoldoutHeaderGroup(SessionState.GetBool(key, true), title);
            SessionState.SetBool(key, open);
            if (open)
            {
                bool enabled = true;
                if (toggle != null)
                {
                    var toggleProp = FindProperty(toggle, properties, false);
                    if (toggleProp != null)
                    {
                        materialEditor.ShaderProperty(toggleProp, Content(toggleProp, "Enabled"));
                        enabled = toggleProp.hasMixedValue || toggleProp.floatValue > 0.5f;
                    }
                }

                if (enabled)
                {
                    using (new EditorGUI.IndentLevelScope(toggle != null ? 1 : 0))
                        foreach (var name in names)
                        {
                            var p = FindProperty(name, properties, false);
                            if (p != null) materialEditor.ShaderProperty(p, Content(p, null));
                        }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        static GUIContent Content(MaterialProperty p, string labelOverride) =>
            new GUIContent(labelOverride ?? p.displayName, Tooltips.TryGetValue(p.name, out var tip) ? tip : null);
    }
}
