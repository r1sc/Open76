using UnityEngine;

namespace Assets.Scripts.System.Fileparsers
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
        public static GeoMesh ReadGeoMesh(string fileName)
        {
            using (Scripts.System.FastBinaryReader br = VirtualFilesystem.Instance.GetFileStream(fileName))
            {
                GeoMesh mesh = new GeoMesh();
                string magic = br.ReadCString(4);
                uint unk1 = br.ReadUInt32();
                mesh.Name = br.ReadCString(16);
                uint vertexCount = br.ReadUInt32();
                uint faceCount = br.ReadUInt32();
                uint unk2 = br.ReadUInt32();

                mesh.Vertices = new Vector3[vertexCount];
                for (int i = 0; i < vertexCount; i++)
                {
                    Vector3 vertex = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    mesh.Vertices[i] = vertex;
                }

                mesh.Normals = new Vector3[vertexCount];
                for (int i = 0; i < vertexCount; i++)
                {
                    Vector3 normal = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    mesh.Normals[i] = normal;
                }

                mesh.Faces = new GeoFace[faceCount];
                for (int i = 0; i < faceCount; i++)
                {
                    GeoFace face = new GeoFace();
                    face.Index = br.ReadUInt32();
                    uint numVerticesInFace = br.ReadUInt32();
                    face.Color = new Color32(br.ReadByte(), br.ReadByte(), br.ReadByte(), 255);
                    face.SurfaceNormal = new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    uint unk3 = br.ReadUInt32();
                    face.SurfaceFlags1 = br.ReadByte();
                    face.SurfaceFlags2 = br.ReadByte();
                    face.SurfaceFlags3 = br.ReadByte();
                    string textureName = br.ReadCString(13);
                    if (textureName != "")
                        face.TextureName = textureName;
                    uint unk4 = br.ReadUInt32();
                    uint unk5 = br.ReadUInt32();
                    //Debug.Log("Surf " + face.TextureName + " flags: " + face.SurfaceFlags1 + ", " + face.SurfaceFlags2 + ", " + face.SurfaceFlags3 + ", " + unk3 + ", " + unk4 + ", " + unk5 + ", color=" + face.Color);

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
                return mesh;
            }
        }
    }
}
