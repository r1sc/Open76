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

        void Start()
        {
            
        }
        
        //private Dictionary<string, Sdf> _sdfCache = new Dictionary<string, Sdf>();
        //private Dictionary<string, GameObject> _geoCache = new Dictionary<string, GameObject>();
        public Material GetMaterial(string textureName)
        {
            if (!_materials.ContainsKey(textureName))
            {

                Material material;
                if (VirtualFilesystem.Instance.FileExists(textureName + ".vqm"))
                {
                    var texture = TextureParser.ReadVqmTexture(textureName + ".vqm", Palette);
                    material = Object.Instantiate(texture.alphaIsTransparency ? TransparentMaterialPrefab : TextureMaterialPrefab);
                    material.mainTexture = texture;
                }
                else if (VirtualFilesystem.Instance.FileExists(textureName + ".map"))
                {
                    material = Object.Instantiate(TextureMaterialPrefab);
                    material.mainTexture = TextureParser.ReadMapTexture(textureName + ".map", Palette);
                }
                else
                {
                    throw new Exception("Texture not found: " + textureName);
                }
                _materials[textureName] = material;
            }

            return _materials[textureName];
        }

        private Material GetMaterial(GeoFace geoFace)
        {
            var matName = geoFace.TextureName ?? "color" + geoFace.Color;
            if (!_materials.ContainsKey(matName))
            {
                if (geoFace.TextureName != null)
                {
                    return GetMaterial(geoFace.TextureName);
                }

                var material = Object.Instantiate(ColorMaterialPrefab);
                material.color = geoFace.Color;
                _materials[matName] = material;
            }

            return _materials[matName];
        }

        public GameObject ImportGeo(string geoFile)
        {

            //if (_geoCache.ContainsKey(geoFile))
            //{
            //    var objCopy = Object.Instantiate(_geoCache[geoFile]);
            //    while (objCopy.transform.childCount > 0)
            //        Object.Destroy(objCopy.transform.GetChild(0));
            //    return objCopy;
            //}

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
            //_geoCache.Add(geoFile, obj);
            return obj;
        }

        public GameObject ImportSdf(string filename, Transform parent, Vector3 localPosition, Quaternion rotation)
        {
            var sdf = SdfObjectParser.LoadSdf(filename);

            var sdfObject = new GameObject(sdf.Name);
            sdfObject.transform.parent = parent;
            sdfObject.transform.localPosition = localPosition;
            sdfObject.transform.rotation = rotation;

            var partDict = new Dictionary<string, GameObject> { { "WORLD", sdfObject } };

            foreach (var sdfPart in sdf.Parts)
            {
                var partObj = ImportGeo(sdfPart.Name + ".geo");
                partObj.transform.parent = partDict[sdfPart.ParentName].transform;
                partObj.transform.localPosition = sdfPart.Position;
                partObj.transform.localRotation = Quaternion.identity;
                partDict.Add(sdfPart.Name, partObj);
            }

            return sdfObject;
        }

        public void ClearCache()
        {
            _materials.Clear();
        }
    }
}
