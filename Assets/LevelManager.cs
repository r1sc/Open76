using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Fileparsers;
using UnityEngine;

namespace Assets
{
    class LevelManager
    {
        public Material TextureMaterialPrefab;

        private Dictionary<string, Material> _materials = new Dictionary<string, Material>();
        public Color32[] Palette;

        private Material GetMaterial(GeoFace geoFace)
        {
            var matName = geoFace.TextureName ?? "color" + geoFace.Color;
            if (!_materials.ContainsKey(matName))
            {

                var material = GameObject.Instantiate(TextureMaterialPrefab);
                if (geoFace.TextureName != null)
                {
                    if(VirtualFilesystem.Instance.FileExists(geoFace.TextureName + ".vqm"))
                        material.mainTexture = VqmTextureParser.ReadVqmTexture(geoFace.TextureName + ".vqm", Palette);
                    else if(VirtualFilesystem.Instance.FileExists(geoFace.TextureName + ".map"))
                        material.mainTexture = MapTextureParser.ReadMapTexture(geoFace.TextureName + ".map", Palette);
                    else
                    {
                        throw new Exception("Texture not found: " + geoFace.TextureName);
                    }
                }
                else
                    material.color = geoFace.Color;
                _materials[matName] = material;
            }

            return _materials[matName];
        }

        public GameObject ImportGeo(string geoFile)
        {
            var geoMesh = GeoParser.ReadGeoMesh(geoFile);

            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();

            var facesGroupedByMaterial = geoMesh.Faces.GroupBy(GetMaterial).ToArray();
            mesh.subMeshCount = facesGroupedByMaterial.Length;
            var submeshTriangles = new Dictionary<Material, List<int>>();
            foreach (var faceGroup in facesGroupedByMaterial)
            {
                submeshTriangles[faceGroup.Key] = new List<int>();
                foreach (var face in faceGroup)
                {
                    var faceIndex = geoMesh.Faces.ToList().IndexOf(face);
                    var viStart = vertices.Count;
                    foreach (var vertexRef in face.VertexRefs)
                    {
                        vertices.Add(geoMesh.Vertices[vertexRef.VertexIndex]);
                        normals.Add(geoMesh.Normals[faceIndex]);
                        uvs.Add(vertexRef.Uv);
                    }

                    var a = geoMesh.Vertices[face.VertexRefs[0].VertexIndex];
                    var b = geoMesh.Vertices[face.VertexRefs[1].VertexIndex];
                    var c = geoMesh.Vertices[face.VertexRefs[2].VertexIndex];
                    var triangleNormal = Utils.GetPlaneNormal(a, b, c);
                    var quat = Quaternion.FromToRotation(triangleNormal, Vector3.forward);

                    var faceVertices = face.VertexRefs.Select(x => (Vector2)(quat * geoMesh.Vertices[x.VertexIndex])).ToArray();
                    var triangulator = new Triangulator(faceVertices);
                    var faceTriangles = triangulator.Triangulate();
                    for (int j = 0; j < faceTriangles.Length; j += 3)
                    {
                        var t1 = faceTriangles[j + 0];
                        var t2 = faceTriangles[j + 1];
                        var t3 = faceTriangles[j + 2];
                        submeshTriangles[faceGroup.Key].Add(viStart + t1);
                        submeshTriangles[faceGroup.Key].Add(viStart + t2);
                        submeshTriangles[faceGroup.Key].Add(viStart + t3);
                    }
                }
            }

            mesh.vertices = vertices.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.normals = normals.ToArray();
            var i = 0;
            foreach (var submeshTriangle in submeshTriangles)
            {
                mesh.SetTriangles(submeshTriangles[submeshTriangle.Key].ToArray(), i);
                i++;
            }
            mesh.RecalculateBounds();

            var obj = new GameObject(geoMesh.Name);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            obj.AddComponent<MeshRenderer>().materials = facesGroupedByMaterial.Select(x => x.Key).ToArray();
            return obj;
        }

        public void ImportSdf(string filename)
        {
            var sdf = SdfObjectParser.LoadSdf(filename);
            var partDict = new Dictionary<string, GameObject>();

            var root = new GameObject(sdf.Name);
            partDict.Add("WORLD", root);

            foreach (var sdfPart in sdf.Parts)
            {
                var partObj = ImportGeo(sdfPart.Name + ".geo");
                partObj.transform.parent = partDict[sdfPart.ParentName].transform;
                partObj.transform.localPosition = sdfPart.Position;
                partDict.Add(sdfPart.Name, partObj);
            }
        }
    }
}
