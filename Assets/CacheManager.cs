using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Fileparsers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Assets
{
    class CacheManager : MonoBehaviour
    {
        public Material ColorMaterialPrefab;
        public Material TextureMaterialPrefab;
        public Material TransparentMaterialPrefab;

        public Color32[] Palette;

        private readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>();

        void Awake()
        {
            _materials["default"] = Instantiate(TextureMaterialPrefab);
        }

        public Material GetTextureMaterial(string textureName, bool transparent)
        {
            if (!_materials.ContainsKey(textureName))
            {
                Texture2D texture;
                if (VirtualFilesystem.Instance.FileExists(textureName + ".vqm"))
                {
                    texture = TextureParser.ReadVqmTexture(textureName + ".vqm", Palette);
                }
                else if (VirtualFilesystem.Instance.FileExists(textureName + ".map"))
                {
                    texture = TextureParser.ReadMapTexture(textureName + ".map", Palette);
                }
                else
                {
                    throw new Exception("Texture not found: " + textureName);
                }
                var material = Instantiate(transparent ? TransparentMaterialPrefab : TextureMaterialPrefab);
                material.mainTexture = texture;
                material.name = textureName;
                _materials[textureName] = material;
            }

            return _materials[textureName];
        }

        public Material GetColorMaterial(string materialName, Color32 color)
        {
            if (_materials.ContainsKey(materialName))
                return _materials[materialName];

            var material = Instantiate(ColorMaterialPrefab);
            material.color = color;
            _materials[materialName] = material;

            return material;
        }

        private Material GetMaterial(GeoFace geoFace, Vtf vtf)
        {
            
            if (geoFace.TextureName != null)
            {
                if (vtf != null && geoFace.TextureName.StartsWith("V"))
                {
                    Debug.Log("Vehicle tmt reference: " + geoFace.TextureName);
                    switch (geoFace.TextureName)
                    {
                        case "V1 BO DY":
                            geoFace.TextureName = vtf.Maps[12];
                            break;
                        case "V5 FT TP":
                            geoFace.TextureName = vtf.Maps[1];
                            break;
                        default:
                            geoFace.TextureName = vtf.Maps[0];
                            break;
                    }
                }
                return GetTextureMaterial(geoFace.TextureName, geoFace.SurfaceFlags2 == 5 || geoFace.SurfaceFlags2 == 7);
                //Debug.Log(geoFace.TextureName + "color=" + geoFace.Color + " flag1=" + geoFace.SurfaceFlags1 + " flag2=" + geoFace.SurfaceFlags2, mat);
            }
            return GetColorMaterial("color" + geoFace.Color, geoFace.Color);
        }
        


        public GameObject ImportGeo(string filename, Vtf vtf)
        {
            var geoMesh = GeoParser.ReadGeoMesh(filename);

            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();

            var facesGroupedByMaterial = geoMesh.Faces.GroupBy(face => GetMaterial(face, vtf)).ToArray();
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
                        normals.Add(geoMesh.Normals[faceIndex % geoMesh.Normals.Length]);
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


        private Dictionary<string, GameObject> _sdfCache = new Dictionary<string, GameObject>();
        public GameObject ImportSdf(string filename, Transform parent, Vector3 localPosition, Quaternion rotation)
        {
            if (_sdfCache.ContainsKey(filename))
            {
                var obj = Instantiate(_sdfCache[filename], parent);
                obj.transform.localPosition = localPosition;
                obj.transform.localRotation = rotation;
                return obj;
            }

            var sdf = SdfObjectParser.LoadSdf(filename);

            var sdfObject = new GameObject(sdf.Name);
            sdfObject.transform.parent = parent;
            sdfObject.transform.localPosition = localPosition;
            sdfObject.transform.rotation = rotation;

            var partDict = new Dictionary<string, GameObject> { { "WORLD", sdfObject } };

            foreach (var sdfPart in sdf.Parts)
            {
                var partObj = ImportGeo(sdfPart.Name + ".geo", null);
                partObj.transform.parent = partDict[sdfPart.ParentName].transform;
                partObj.transform.localPosition = sdfPart.Position;
                partObj.transform.localRotation = Quaternion.identity;
                partDict.Add(sdfPart.Name, partObj);
            }

            _sdfCache.Add(filename, sdfObject);
            return sdfObject;
        }

        public GameObject ImportVcf(string filename)
        {
            var vcf = VcfParser.ParseVcf(filename);
            var vdf = VdfParser.ParseVdf(vcf.VdfFilename);
            var vtf = VtfParser.ParseVtf(vcf.VtfFilename);
            


            var vdfObject = new GameObject(vdf.Name);
            var partDict = new Dictionary<string, GameObject> { { "WORLD", vdfObject } };

            foreach (var sdfPart in vdf.PartsFirstPerson)
            {
                if (sdfPart.Name == "NULL" || sdfPart.Name.EndsWith("3"))
                    continue;
                var partObj = ImportGeo(sdfPart.Name + ".geo", vtf);
                var parentName = sdfPart.ParentName;
                if (!partDict.ContainsKey(parentName))
                {
                    Debug.Log("Cant find parent '" + sdfPart.ParentName + "' for '" + sdfPart.Name + "'");
                    parentName = "WORLD";
                }
                partObj.transform.parent = partDict[parentName].transform;
                partObj.transform.right = sdfPart.Right;
                partObj.transform.up = sdfPart.Up;
                partObj.transform.forward = sdfPart.Forward;
                partObj.transform.localPosition = sdfPart.Position;
                partDict.Add(sdfPart.Name, partObj);
            }


            return vdfObject;
        }

        public void ClearCache()
        {
            _materials.Clear();
        }
    }
}
