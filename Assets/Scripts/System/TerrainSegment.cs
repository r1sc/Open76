using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.System
{
    public class TerrainSegment : MonoBehaviour
    {
        public int Width = 128;
        public int Depth = 128;

        private void Awake()
        {
            if (GetComponent<MeshFilter>().mesh == null)
            {
                GenerateMesh(null);
            }
        }

        private void GenerateMesh(float[,] heights)
        {
            Mesh mesh = new Mesh();
            List<Vector3> vertices = new List<Vector3>();
            List<int> indices = new List<int>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            int idx = 0;
            for (int z = 0; z < Depth; z++)
            {
                for (int x = 0; x < Width; x++)
                {

                    vertices.Add(new Vector3(x, 0, z));
                    normals.Add(Vector3.up);
                    uvs.Add(new Vector2(x, z));
                }
            }

            for (int z = 0; z < Depth - 1; z++)
            {
                for (int x = 0; x < Width - 1; x++)
                {
                    indices.Add(idx);
                    indices.Add(idx + Width);
                    indices.Add(idx + 1);

                    indices.Add(idx + Width + 1);
                    indices.Add(idx + 1);
                    indices.Add(idx + Width);

                    idx++;
                }
                idx++;
            }
            mesh.vertices = vertices.ToArray();
            mesh.triangles = indices.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uvs.ToArray();
            GetComponent<MeshFilter>().mesh = mesh;
        }

        public void SetHeights(float[,] heights)
        {
            //if(GetComponent<MeshFilter>().mesh == null)
            //    GenerateMesh();
            Mesh mesh = GetComponent<MeshFilter>().mesh;
            Vector3[] vertices = mesh.vertices;
            for (int z = 0; z < Depth; z++)
            {
                for (int x = 0; x < Width; x++)
                {
                    vertices[z * Width + x].y = heights[x, z];
                }
            }
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
        }
    }
}