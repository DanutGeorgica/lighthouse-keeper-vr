using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HardArtcore.ShoreLine
{
    /// <summary>
    /// Builds flat water meshes in the XZ plane, centered on the origin, facing up.
    /// Layout contract (WaterSurface relies on it): the inner grid's vertices come first, row by row from the
    /// -x/-z corner, so vertex 0 is that corner and vertex 1 is one grid step along +x.
    /// </summary>
    public static class WaterMeshBuilder
    {
        /// <summary>A square grid of <paramref name="resolution"/> x <paramref name="resolution"/> quads.</summary>
        public static Mesh BuildGrid(float size, int resolution)
        {
            Validate(size, resolution);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            AddGrid(size, resolution, vertices, triangles);
            return CreateMesh("Water Grid", vertices, triangles, size);
        }

        /// <summary>
        /// The inner grid plus one ring of long quads reaching <paramref name="outerSize"/>.
        /// Each of the grid's 4*resolution border vertices is shared with the ring, so there are no T-junctions.
        /// </summary>
        public static Mesh BuildOcean(float innerSize, int innerResolution, float outerSize)
        {
            Validate(innerSize, innerResolution);
            if (outerSize <= innerSize)
                throw new ArgumentOutOfRangeException(nameof(outerSize), outerSize, "Outer size must be larger than inner size.");

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            AddGrid(innerSize, innerResolution, vertices, triangles);

            // Border vertex indices in counter-clockwise order seen from above, starting at the -x/-z corner.
            int row = innerResolution + 1;
            var border = new List<int>(4 * innerResolution);
            for (int x = 0; x < innerResolution; x++) border.Add(x);                         // bottom edge, towards +x
            for (int z = 0; z < innerResolution; z++) border.Add(z * row + innerResolution); // right edge, towards +z
            for (int x = innerResolution; x > 0; x--) border.Add(innerResolution * row + x); // top edge, towards -x
            for (int z = innerResolution; z > 0; z--) border.Add(z * row);                   // left edge, towards -z

            // One outer vertex per border vertex, projected radially onto the outer square.
            float outerHalf = outerSize * 0.5f;
            int outerStart = vertices.Count;
            foreach (int b in border)
            {
                var p = vertices[b];
                vertices.Add(p * (outerHalf / Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.z))));
            }

            for (int i = 0; i < border.Count; i++)
            {
                int next = (i + 1) % border.Count;
                int b0 = border[i], b1 = border[next];
                int o0 = outerStart + i, o1 = outerStart + next;
                triangles.Add(b0); triangles.Add(b1); triangles.Add(o0);
                triangles.Add(b1); triangles.Add(o1); triangles.Add(o0);
            }

            return CreateMesh("Water Ocean", vertices, triangles, innerSize);
        }

        static void Validate(float size, int resolution)
        {
            if (size <= 0f) throw new ArgumentOutOfRangeException(nameof(size), size, "Size must be positive.");
            if (resolution < 1) throw new ArgumentOutOfRangeException(nameof(resolution), resolution, "Resolution must be at least 1.");
        }

        static void AddGrid(float size, int resolution, List<Vector3> vertices, List<int> triangles)
        {
            int row = resolution + 1;
            float half = size * 0.5f, step = size / resolution;
            for (int z = 0; z <= resolution; z++)
                for (int x = 0; x <= resolution; x++)
                    vertices.Add(new Vector3(x * step - half, 0f, z * step - half));

            for (int z = 0; z < resolution; z++)
                for (int x = 0; x < resolution; x++)
                {
                    int i = z * row + x;
                    triangles.Add(i); triangles.Add(i + row); triangles.Add(i + 1);
                    triangles.Add(i + 1); triangles.Add(i + row); triangles.Add(i + row + 1);
                }
        }

        static Mesh CreateMesh(string name, List<Vector3> vertices, List<int> triangles, float uvSize)
        {
            var normals = new Vector3[vertices.Count];
            var uvs = new Vector2[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
            {
                normals[i] = Vector3.up;
                uvs[i] = new Vector2(vertices[i].x / uvSize + 0.5f, vertices[i].z / uvSize + 0.5f);
            }

            var mesh = new Mesh { name = name };
            mesh.indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
