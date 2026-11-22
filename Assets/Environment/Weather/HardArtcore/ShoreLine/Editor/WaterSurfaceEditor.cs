using UnityEditor;
using UnityEngine;

namespace HardArtcore.ShoreLine.Editor
{
    [CustomEditor(typeof(WaterSurface))]
    public class WaterSurfaceEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var ws = (WaterSurface)target;

            if (ws.OceanMode && ws.GridStep <= 0f)
                EditorGUILayout.HelpBox("Ocean Mode needs a mesh made with Tools > HardArtcore > ShoreLine > Create Water Mesh " +
                                        "(Ocean Ring on), with Read/Write enabled.", MessageType.Warning);
            else if (!ws.WaveFadeFitsGrid)
                EditorGUILayout.HelpBox($"Waves reach past the fine inner grid ({ws.InnerHalfSize - ws.GridStep * 0.5f:0.#} m). " +
                                        "Lower the material's Wave Fade End or use a larger inner grid, or distant waves will look smeared.",
                                        MessageType.Warning);
        }

        // Play mode parity check: spheres at SampleHeight should sit on the visible wave surface.
        [DrawGizmo(GizmoType.Selected)]
        static void DrawHeightGizmos(WaterSurface ws, GizmoType type)
        {
            if (!Application.isPlaying) return;
            Gizmos.color = Color.yellow;
            var center = ws.transform.position;
            for (int x = -2; x <= 2; x++)
                for (int z = -2; z <= 2; z++)
                {
                    var p = center + new Vector3(x * 2f, 0f, z * 2f);
                    p.y = ws.SampleHeight(p);
                    Gizmos.DrawSphere(p, 0.15f);
                }
        }
    }
}
