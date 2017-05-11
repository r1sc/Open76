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
        public GameObject _3DObjectPrefab;
        public GameObject NoColliderPrefab;
        public ArcadeWheel SteerWheelPrefab;
        public ArcadeWheel DriveWheelPrefab;
        public GameObject CarBodyPrefab;
        public ArcadeCar CarPrefab;

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
                var textureName = geoFace.TextureName;
                if (vtf != null && geoFace.TextureName.StartsWith("V"))
                {
                    if (geoFace.TextureName.EndsWith("BO DY"))
                    {
                        textureName = vtf.Maps[12];
                    }
                    else
                    {
                        var key = geoFace.TextureName.Substring(1).Replace(" ", "").Replace("LF", "LT") + ".TMT";

                        if (vtf.Tmts.ContainsKey(key))
                        {
                            //Debug.Log("Vehicle tmt reference: " + geoFace.TextureName + " decoded: " + key);
                            var tmt = vtf.Tmts[key];
                            textureName = tmt.TextureNames[0];
                        }
                    }
                }
                return GetTextureMaterial(textureName, geoFace.SurfaceFlags2 == 5 || geoFace.SurfaceFlags2 == 7);
                //Debug.Log(geoFace.TextureName + "color=" + geoFace.Color + " flag1=" + geoFace.SurfaceFlags1 + " flag2=" + geoFace.SurfaceFlags2, mat);
            }
            return GetColorMaterial("color" + geoFace.Color, geoFace.Color);
        }

        public GameObject ImportGeo(string filename, Vtf vtf, GameObject prefab)
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
                    var numTriangles = face.VertexRefs.Length - 3 + 1;
                    var viStart = vertices.Count;
                    foreach (var vertexRef in face.VertexRefs)
                    {
                        vertices.Add(geoMesh.Vertices[vertexRef.VertexIndex]);
                        normals.Add(geoMesh.Normals[vertexRef.VertexIndex] * -1);
                        uvs.Add(vertexRef.Uv);
                    }
                    for (var t = 1; t <= numTriangles; t++)
                    {
                        submeshTriangles[faceGroup.Key].Add(viStart + t);
                        submeshTriangles[faceGroup.Key].Add(viStart);
                        submeshTriangles[faceGroup.Key].Add(viStart + t + 1);
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

            var obj = Instantiate(prefab);
            obj.gameObject.name = geoMesh.Name;

            obj.GetComponent<MeshFilter>().sharedMesh = mesh;
            obj.GetComponent<MeshRenderer>().materials = facesGroupedByMaterial.Select(x => x.Key).ToArray();
            var collider = obj.GetComponent<MeshCollider>();
            if (collider != null)
                collider.sharedMesh = mesh;

            return obj.gameObject;
        }


        private readonly Dictionary<string, GameObject> _sdfCache = new Dictionary<string, GameObject>();
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
                var partObj = ImportGeo(sdfPart.Name + ".geo", null, _3DObjectPrefab);
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

            var carObject = Instantiate(CarPrefab);
            carObject.gameObject.name = vdf.Name + " (" + vcf.VariantName + ")";
            ImportCarParts(carObject.gameObject, vtf, vdf.PartsThirdPerson[0]);
            //ImportCarParts(carObject, vtf, vdf.PartsFirstPerson);
            //var destroyed = ImportCarParts(car, vtf, vdf.PartsThirdPerson[3]);
            //destroyed.gameObject.SetActive(false);

            if (vcf.FrontWheelDef != null)
            {
                var frontWheels = CreateWheelPair("Front", 0, carObject.gameObject, vdf, vtf, vcf.FrontWheelDef.Parts);
                carObject.SteerWheels = frontWheels;
            }
            if (vcf.MidWheelDef != null)
            {
                CreateWheelPair("Mid", 2, carObject.gameObject, vdf, vtf, vcf.MidWheelDef.Parts);
            }
            if (vcf.BackWheelDef != null)
            {
                var backWheels = CreateWheelPair("Back", 4, carObject.gameObject, vdf, vtf, vcf.BackWheelDef.Parts);
                carObject.DriveWheels = backWheels;
            }

            return carObject.gameObject;
        }

        private ArcadeWheel[] CreateWheelPair(string placement, int wheelIndex, GameObject car, Vdf vdf, Vtf vtf, SdfPart[] parts)
        {
            var wheel1Name = placement + "Right";
            var wheel = Instantiate(placement == "Front" ? SteerWheelPrefab : DriveWheelPrefab, car.transform);
            wheel.gameObject.name = wheel1Name;
            ImportCarParts(wheel.transform.Find("Mesh").gameObject, vtf, parts, true);
            wheel.transform.localPosition = vdf.WheelLoc[wheelIndex].Position;

            var wheel2 = Instantiate(wheel, car.transform);
            wheel2.name = placement + "Left";
            wheel2.transform.localPosition = vdf.WheelLoc[wheelIndex + 1].Position;
            wheel2.transform.Find("Mesh").localScale = new Vector3(-1, 1, 1);

            return new[] { wheel, wheel2 };
        }

        private GameObject ImportCarParts(GameObject parent, Vtf vtf, SdfPart[] sdfParts, bool wheel = false)
        {
            var partDict = new Dictionary<string, GameObject> { { "WORLD", parent } };
            GameObject firstObject = null;

            foreach (var sdfPart in sdfParts)
            {
                if (sdfPart.Name == "NULL")
                    continue;

                GameObject prefab = NoColliderPrefab;
                if (!wheel && sdfPart.Name.Substring(0, sdfPart.Name.Length - 1).EndsWith("BDY"))
                    prefab = CarBodyPrefab;

                var partObj = ImportGeo(sdfPart.Name + ".geo", vtf, prefab);
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
                firstObject = partObj;
            }

            return firstObject;
        }

        public void ClearCache()
        {
            _materials.Clear();
        }
    }
}
