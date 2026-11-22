using UnityEditor;
using UnityEngine;

namespace HardArtcore.ShoreLine.Editor
{
    /// <summary>Tools menu window that generates water grid / ocean meshes as assets.</summary>
    public class WaterMeshWindow : EditorWindow
    {
        float size = 50f;
        int resolution = 64;
        bool ocean;
        float outerSize = 2000f;

        [MenuItem("Tools/HardArtcore/ShoreLine/Create Water Mesh")]
        static void Open() => GetWindow<WaterMeshWindow>("Water Mesh");

        void OnGUI()
        {
            size = EditorGUILayout.FloatField(new GUIContent("Size", "Width of the (inner) grid in meters."), size);
            resolution = EditorGUILayout.IntSlider(new GUIContent("Resolution", "Quads per side. More quads give smoother waves but cost more vertices."), resolution, 1, 512);
            ocean = EditorGUILayout.Toggle(new GUIContent("Ocean Ring", "Adds a low-detail ring reaching to the horizon. Use with Ocean Mode on WaterSurface."), ocean);
            using (new EditorGUI.DisabledScope(!ocean))
                outerSize = EditorGUILayout.FloatField(new GUIContent("Outer Size", "Total width including the ring, in meters."), outerSize);

            int vertexCount = (resolution + 1) * (resolution + 1) + (ocean ? 4 * resolution : 0);
            EditorGUILayout.HelpBox($"{vertexCount:N0} vertices", MessageType.None);

            bool valid = size > 0f && (!ocean || outerSize > size);
            if (!valid) EditorGUILayout.HelpBox("Size must be positive and Outer Size larger than Size.", MessageType.Warning);

            using (new EditorGUI.DisabledScope(!valid))
                if (GUILayout.Button("Create Mesh Asset"))
                    Create();
        }

        void Create()
        {
            string defaultName = ocean ? $"WaterOcean_{size:0}_{resolution}" : $"WaterGrid_{size:0}_{resolution}";
            string path = EditorUtility.SaveFilePanelInProject("Save Water Mesh", defaultName, "asset", "Choose where to save the mesh.");
            if (string.IsNullOrEmpty(path)) return;

            var mesh = ocean ? WaterMeshBuilder.BuildOcean(size, resolution, outerSize) : WaterMeshBuilder.BuildGrid(size, resolution);
            AssetDatabase.CreateAsset(mesh, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(mesh);
        }
    }
}
