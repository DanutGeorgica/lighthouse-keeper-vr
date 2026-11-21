
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class LighthouseBeamMesh : MonoBehaviour
{
    [Header("Beam Shape")]
    [SerializeField] private float length = 100f;
    [SerializeField] private float horizontalHalfAngle = 6f;
    [SerializeField] private float verticalHalfAngle = 0.6f;

    [Header("Mesh Quality")]
    [SerializeField, Range(8, 64)] private int radialSegments = 24;
    [SerializeField, Range(4, 64)] private int lengthSegments = 32;

    [Header("Transparency Fade")]
    [SerializeField, Range(0f, 1f)] private float maximumAlpha = 0.16f;
    [SerializeField] private float fadeExponent = 1.3f;

    private Mesh generatedMesh;
    private MeshFilter meshFilter;

    private void Awake()
    {
        GenerateBeam();
    }

    [ContextMenu("Regenerate Beam Mesh")]
    public void GenerateBeam()
    {
        meshFilter = GetComponent<MeshFilter>();

        if (generatedMesh != null)
        {
            if (Application.isPlaying)
                Destroy(generatedMesh);
            else
                DestroyImmediate(generatedMesh);
        }

        generatedMesh = new Mesh();
        generatedMesh.name = "Lighthouse Beam Mesh";

        int ringSize = radialSegments + 1;
        int vertexCount = (lengthSegments + 1) * ringSize;

        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uv = new Vector2[vertexCount];
        Color[] colors = new Color[vertexCount];

        int[] triangles = new int[lengthSegments * radialSegments * 6];

        float horizontalSpread =
            Mathf.Tan(horizontalHalfAngle * Mathf.Deg2Rad);

        float verticalSpread =
            Mathf.Tan(verticalHalfAngle * Mathf.Deg2Rad);

        // Generate the expanding elliptical beam.
        for (int i = 0; i <= lengthSegments; i++)
        {
            float t = (float)i / lengthSegments;

            float distance = Mathf.Lerp(0.01f, length, t);

            // Local X corresponds to world vertical (Z).
            float radiusX = distance * verticalSpread;

            // Local Y corresponds to horizontal width around the horizon.
            float radiusY = distance * horizontalSpread;

            // Fade the beam gradually towards its end.
            float alpha = maximumAlpha *
                Mathf.Pow(1f - t, fadeExponent);

            for (int j = 0; j <= radialSegments; j++)
            {
                float angle = (float)j / radialSegments * Mathf.PI * 2f;

                int index = i * ringSize + j;

                vertices[index] = new Vector3(
                    Mathf.Cos(angle) * radiusX,
                    Mathf.Sin(angle) * radiusY,
                    distance
                );

                uv[index] = new Vector2(
                    (float)j / radialSegments,
                    t
                );

                // The shader will use this alpha value.
                colors[index] = new Color(1f, 1f, 1f, alpha);
            }
        }

        // Connect the rings to create the beam surface.
        int triangleIndex = 0;

        for (int i = 0; i < lengthSegments; i++)
        {
            for (int j = 0; j < radialSegments; j++)
            {
                int a = i * ringSize + j;
                int b = a + 1;
                int c = (i + 1) * ringSize + j;
                int d = c + 1;

                triangles[triangleIndex++] = a;
                triangles[triangleIndex++] = c;
                triangles[triangleIndex++] = b;

                triangles[triangleIndex++] = b;
                triangles[triangleIndex++] = c;
                triangles[triangleIndex++] = d;
            }
        }

        generatedMesh.vertices = vertices;
        generatedMesh.uv = uv;
        generatedMesh.colors = colors;
        generatedMesh.triangles = triangles;

        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();

        meshFilter.sharedMesh = generatedMesh;
    }

    private void OnDestroy()
    {
        if (generatedMesh == null)
            return;

        if (Application.isPlaying)
            Destroy(generatedMesh);
        else
            DestroyImmediate(generatedMesh);
    }

    private void OnValidate()
    {
        length = Mathf.Max(1f, length);
        horizontalHalfAngle = Mathf.Clamp(horizontalHalfAngle, 0.1f, 30f);
        verticalHalfAngle = Mathf.Clamp(verticalHalfAngle, 0.1f, 10f);
        fadeExponent = Mathf.Max(0.1f, fadeExponent);
    }
}
