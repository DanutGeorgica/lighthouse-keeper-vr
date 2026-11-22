using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HardArtcore.ShoreLine.Editor
{
    /// <summary>Checks and installs ShoreLineFeature on the project's URP renderers.</summary>
    public static class ShoreLineSetup
    {
        /// <summary>True when the renderer has an active ShoreLineFeature (a switched-off one requests no depth).</summary>
        public static bool IsInstalled(UniversalRendererData data) =>
            data != null && data.rendererFeatures.Exists(f => f is ShoreLineFeature && f.isActive);

        /// <summary>Adds the feature to the renderer once, or switches an existing one back on. Safe to call repeatedly.</summary>
        public static void Install(UniversalRendererData data)
        {
            if (data == null || IsInstalled(data)) return;

            var existing = data.rendererFeatures.Find(f => f is ShoreLineFeature);
            if (existing != null)
            {
                Undo.RecordObject(existing, "Enable ShoreLine Feature");
                existing.SetActive(true);
                EditorUtility.SetDirty(existing);
                data.SetDirty();
                return;
            }

            var feature = ScriptableObject.CreateInstance<ShoreLineFeature>();
            feature.name = nameof(ShoreLineFeature);

            if (AssetDatabase.Contains(data))
            {
                Undo.RegisterCreatedObjectUndo(feature, "Add ShoreLine Feature");
                Undo.RecordObject(data, "Add ShoreLine Feature");
                AssetDatabase.AddObjectToAsset(feature, data);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);
                var so = new SerializedObject(data);
                var features = so.FindProperty("m_RendererFeatures");
                var map = so.FindProperty("m_RendererFeatureMap");
                features.arraySize++;
                features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;
                map.arraySize++;
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssetIfDirty(data);
            }
            else
            {
                data.rendererFeatures.Add(feature);
            }
            data.SetDirty();
        }

        /// <summary>URP renderers used by the default pipeline asset and every quality level.</summary>
        public static IEnumerable<UniversalRendererData> ActiveRenderers()
        {
            var assets = new List<UniversalRenderPipelineAsset>();
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset main) assets.Add(main);
            for (int i = 0; i < QualitySettings.count; i++)
                if (QualitySettings.GetRenderPipelineAssetAt(i) is UniversalRenderPipelineAsset q && !assets.Contains(q))
                    assets.Add(q);

            var seen = new HashSet<UniversalRendererData>();
            foreach (var asset in assets)
            {
                var list = new SerializedObject(asset).FindProperty("m_RendererDataList");
                for (int i = 0; i < list.arraySize; i++)
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue is UniversalRendererData data && seen.Add(data))
                        yield return data;
            }
        }

        /// <summary>
        /// URP Compatibility Mode (Render Graph disabled): the feature only implements Render Graph, so it does nothing.
        /// The setting is read-only from Unity 6.3 and gone in 6.4.
        /// </summary>
        public static bool CompatibilityModeOn =>
#if UNITY_6000_3_OR_NEWER
            false;
#else
            GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>() is { enableRenderCompatibilityMode: true };
#endif

        public static IEnumerable<UniversalRendererData> MissingFeature(IEnumerable<UniversalRendererData> renderers) =>
            renderers.Where(r => !IsInstalled(r));
    }
}
