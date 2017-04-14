using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Fileparsers
{
    public class GeoMesh
    {
        public string Name { get; set; }
        public Vector3[] Vertices { get; set; }
        public Vector3[] Normals { get; set; }
        public GeoFace[] Faces { get; set; }
    }

    public class GeoFace
    {
        public uint Index { get; set; }
        public Color32 Color { get; set; }
        public Vector4 SurfaceNormal { get; set; }
        public byte SurfaceFlags1 { get; set; }
        public byte SurfaceFlags2 { get; set; }
        public byte SurfaceFlags3 { get; set; }
        public string TextureName { get; set; }

        public GeoVertexRef[] VertexRefs { get; set; }
    }

    public class GeoVertexRef
    {
        public uint VertexIndex { get; set; }
        public uint NormalIndex { get; set; }
        public Vector2 Uv { get; set; }
    }

    public class GeoParser
    {
        private static readonly Dictionary<string, GeoMesh> GeoMeshCache = new Dictionary<string, GeoMesh>();

        public static GeoMesh ReadGeoMesh(string filename)
        {
            if (GeoMeshCache.ContainsKey(filename))
                return GeoMeshCache[filename];

            using (var br = new BinaryReader(VirtualFilesystem.Instance.GetFileStream(filename)))
            {
                var mesh = new GeoMesh();
                var magic = new string(br.ReadChars(4));
                var unk1 = br.ReadUInt32();
                mesh.Name = new string(br.ReadChars(16)).TrimEnd('\0');
                var vertexCount = br.ReadUInt32();
                var faceCount = br.ReadUInt32();
                var unk2 = br.ReadUInt32();

                mesh.Vertices = new Vector3[vertexCount];
                for (int i = 0; i < vertexCount; i++)
                {
                    var vertex = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    mesh.Vertices[i] = vertex;
                }

                mesh.Normals = new Vector3[vertexCount];
                for (int i = 0; i < vertexCount; i++)
                {
                    var normal = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    mesh.Normals[i] = normal;
                }

                mesh.Faces = new GeoFace[faceCount];
                for (int i = 0; i < faceCount; i++)
                {
                    var face = new GeoFace();
                    face.Index = br.ReadUInt32();
                    var numVerticesInFace  = br.ReadUInt32();
                    face.Color = new Color32(br.ReadByte(), br.ReadByte(), br.ReadByte(), 255);
                    face.SurfaceNormal = new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    var unk3 = br.ReadUInt32();
                    face.SurfaceFlags1 = br.ReadByte();
                    face.SurfaceFlags2 = br.ReadByte();
                    face.SurfaceFlags3 = br.ReadByte();
                    var textureName = br.ReadChars(13);
                    if(textureName[0] != '\0')
                        face.TextureName = new string(textureName).TrimEnd('\0');
                    var unk4 = br.ReadUInt32();
                    var unk5 = br.ReadUInt32();

                    face.VertexRefs = new GeoVertexRef[numVerticesInFace];
                    for (int v = 0; v < numVerticesInFace; v++)
                    {
                        face.VertexRefs[v] = new GeoVertexRef
                        {
                            VertexIndex = br.ReadUInt32(),
                            NormalIndex = br.ReadUInt32(),
                            Uv = new Vector2(br.ReadSingle(), br.ReadSingle())
                        };
                    }
                    mesh.Faces[i] = face;
                }
                GeoMeshCache.Add(filename, mesh);
                return mesh;
            }
        }
    }
}
