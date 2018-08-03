using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Fileparsers;
using UnityEngine;
using Object = UnityEngine.Object;
using Assets.Car;
using System.IO;

namespace Assets.System
{
    class CacheManager : MonoBehaviour
    {
        public string GamePath;
        public GameObject _3DObjectPrefab;
        public GameObject NoColliderPrefab;
        public RaySusp SteerWheelPrefab;
        public RaySusp DriveWheelPrefab;
        public GameObject CarBodyPrefab;
        public NewCar CarPrefab;

        public Material ColorMaterialPrefab;
        public Material TextureMaterialPrefab;
        public Material TransparentMaterialPrefab;
        public Color32[] Palette;

        public static CacheManager Instance { get; private set; }

        private readonly string[] _bannedNames =
        {
            "51CMP3",
            "51SPC3",
            "51SYS3",
            "51WEP3",
            "51RTC1"
        };

        private static readonly Dictionary<string, GeoMeshCacheEntry> _meshCache = new Dictionary<string, GeoMeshCacheEntry>();
        private readonly Dictionary<string, GameObject> _sdfCache = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, Material> _materialCache = new Dictionary<string, Material>();

        void Start()
        {
            VirtualFilesystem.Instance.Init(GamePath);
            _materialCache["default"] = Instantiate(TextureMaterialPrefab);
            Palette = ActPaletteParser.ReadActPalette("p02.act");
            Instance = this;
        }

        public Texture2D GetTexture(string textureName)
        {
            var filename = Path.GetFileNameWithoutExtension(textureName);
            Texture2D texture = null;
            if (VirtualFilesystem.Instance.FileExists(filename + ".vqm"))
            {
                texture = TextureParser.ReadVqmTexture(filename + ".vqm", Palette);
            }
            else if (VirtualFilesystem.Instance.FileExists(filename + ".map"))
            {
                texture = TextureParser.ReadMapTexture(filename + ".map", Palette);
            }
            else
            {
                Debug.LogWarning("Texture not found: " + textureName);
            }
            return texture;
        }

        public Material GetTextureMaterial(string textureName, bool transparent)
        {
            if (!_materialCache.ContainsKey(textureName))
            {
                var texture = GetTexture(textureName);
                var material = Instantiate(transparent ? TransparentMaterialPrefab : TextureMaterialPrefab);
                material.mainTexture = texture ?? Texture2D.blackTexture;
                material.name = textureName;
                _materialCache[textureName] = material;
            }

            return _materialCache[textureName];
        }

        public Material GetColorMaterial(string materialName, Color32 color)
        {
            if (_materialCache.ContainsKey(materialName))
                return _materialCache[materialName];

            var material = Instantiate(ColorMaterialPrefab);
            material.color = color;
            _materialCache[materialName] = material;

            return material;
        }

        private Material GetMaterial(GeoFace geoFace, Vtf vtf)
        {

            if (geoFace.TextureName != null)
            {
                var textureName = Path.GetFileNameWithoutExtension(geoFace.TextureName);
                if (vtf != null && textureName.StartsWith("V"))
                {
                    if (textureName.EndsWith("BO DY"))
                    {
                        textureName = vtf.Maps[12];
                    }
                    else
                    {
                        var key = textureName.Substring(1).Replace(" ", "").Replace("LF", "LT") + ".TMT";

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

        class GeoMeshCacheEntry
        {
            public GeoMesh GeoMesh { get; set; }
            public Mesh Mesh { get; set; }
            public Material[] Materials { get; set; }
        }

        private GeoMeshCacheEntry ImportMesh(string filename, Vtf vtf)
        {
            if (_meshCache.ContainsKey(filename))
                return _meshCache[filename];

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
            mesh.RecalculateBounds();

            var cacheEntry = new GeoMeshCacheEntry
            {
                GeoMesh = geoMesh,
                Mesh = mesh,
                Materials = facesGroupedByMaterial.Select(x => x.Key).ToArray()
            };
            _meshCache.Add(filename, cacheEntry);

            return cacheEntry;
        }

        public GameObject ImportGeo(string filename, Vtf vtf, GameObject prefab)
        {
            var meshCacheEntry = ImportMesh(filename, vtf);

            var obj = Instantiate(prefab);
            obj.SetActive(false);
            obj.gameObject.name = meshCacheEntry.GeoMesh.Name;

            var meshFilter = obj.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = meshCacheEntry.Mesh;
                obj.GetComponent<MeshRenderer>().materials = meshCacheEntry.Materials;
            }
            var collider = obj.GetComponent<MeshCollider>();
            if (collider != null)
                collider.sharedMesh = meshCacheEntry.Mesh;

            return obj.gameObject;
        }


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
                var geoFilename = sdfPart.Name + ".geo";
                if (!VirtualFilesystem.Instance.FileExists(geoFilename))
                {
                    Debug.LogWarning("File does not exist: " + geoFilename);
                    continue;
                }

                var partObj = ImportGeo(geoFilename, null, _3DObjectPrefab);
                partObj.transform.parent = partDict[sdfPart.ParentName].transform;
                partObj.transform.localPosition = sdfPart.Position;
                partObj.transform.localRotation = Quaternion.identity;
                partObj.SetActive(true);
                partDict.Add(sdfPart.Name, partObj);
            }

            _sdfCache.Add(filename, sdfObject);
            return sdfObject;
        }

        public GameObject ImportVcf(string filename, bool importFirstPerson)
        {
            var vcf = VcfParser.ParseVcf(filename);
            var vdf = VdfParser.ParseVdf(vcf.VdfFilename);
            var vtf = VtfParser.ParseVtf(vcf.VtfFilename);

            var carObject = Instantiate(CarPrefab); //ImportGeo(vdf.SOBJGeoName + ".geo", vtf, CarPrefab.gameObject).GetComponent<ArcadeCar>();
            carObject.gameObject.name = vdf.Name + " (" + vcf.VariantName + ")";

            foreach (var hLoc in vdf.HLocs)
            {
                var hlocGo = new GameObject("HLOC");
                hlocGo.transform.parent = carObject.transform;
                hlocGo.transform.localRotation = Quaternion.LookRotation(hLoc.Forward, hLoc.Up);
                hlocGo.transform.localPosition = hLoc.Position;
            }
            foreach (var vLoc in vdf.VLocs)
            {
                var vlocGo = new GameObject("VLOC");
                vlocGo.transform.parent = carObject.transform;
                vlocGo.transform.localRotation = Quaternion.LookRotation(vLoc.Forward, vLoc.Up);
                vlocGo.transform.localPosition = vLoc.Position;
            }

            var chassis = new GameObject("Chassis");
            chassis.transform.parent = carObject.transform;

            var thirdPerson = new GameObject("ThirdPerson");
            thirdPerson.transform.parent = chassis.transform;

            var firstPerson = new GameObject("FirstPerson");
            firstPerson.transform.parent = chassis.transform;
            firstPerson.SetActive(false);

            ImportCarParts(thirdPerson, vtf, vdf.PartsThirdPerson[0], NoColliderPrefab, false);
            if (importFirstPerson)
                ImportCarParts(firstPerson, vtf, vdf.PartsFirstPerson, NoColliderPrefab, false, true);

            var meshFilters = thirdPerson.GetComponentsInChildren<MeshFilter>();
            var bounds = new Bounds();
            bounds.SetMinMax(Vector3.one * float.MaxValue, Vector3.one * float.MinValue);
            foreach (var meshFilter in meshFilters)
            {
                var min = Vector3.Min(bounds.min, meshFilter.transform.position + meshFilter.sharedMesh.bounds.min) - thirdPerson.transform.position;
                var max = Vector3.Max(bounds.max, meshFilter.transform.position + meshFilter.sharedMesh.bounds.max) - thirdPerson.transform.position;
                bounds.SetMinMax(min, max);
            }

            var chassisCollider = new GameObject("ChassisColliders");
            chassisCollider.transform.parent = carObject.transform;
            ImportCarParts(chassisCollider, vtf, vdf.PartsThirdPerson[0], CarBodyPrefab, true);

            // Note: The following is probably how I76 does collision detection. Two large boxes that encapsulate the entire vehicle.
            // Right now this won't work with Open76's raycast suspension, so I'm leaving this off for now. Investigate in the future.
            //var innerBox = chassisCollider.AddComponent<BoxCollider>();
            //innerBox.center = vdf.BoundsInner.center;
            //innerBox.size = vdf.BoundsInner.size;

            //var outerBox = chassisCollider.AddComponent<BoxCollider>();
            //outerBox.center = vdf.BoundsOuter.center;
            //outerBox.size = vdf.BoundsOuter.size;
            
            if (vcf.FrontWheelDef != null)
            {
                var frontWheels = CreateWheelPair("Front", 0, carObject.gameObject, vdf, vtf, vcf.FrontWheelDef);
                carObject.FrontWheels = frontWheels;
            }
            if (vcf.MidWheelDef != null)
            {
                CreateWheelPair("Mid", 2, carObject.gameObject, vdf, vtf, vcf.MidWheelDef);
            }
            if (vcf.BackWheelDef != null)
            {
                var rearWheels = CreateWheelPair("Back", 4, carObject.gameObject, vdf, vtf, vcf.BackWheelDef);
                carObject.RearWheels = rearWheels;
            }
            carObject.Chassis = chassis.transform;

            return carObject.gameObject;
        }

        private RaySusp[] CreateWheelPair(string placement, int wheelIndex, GameObject car, Vdf vdf, Vtf vtf, Wdf wheelDef)
        {
            var wheel1Name = placement + "Right";
            var wheel = Instantiate(placement == "Front" ? SteerWheelPrefab : DriveWheelPrefab, car.transform);
            wheel.gameObject.name = wheel1Name;
            wheel.WheelRadius = wheelDef.Radius;  // This is not correct - find out where the radius is really stored
            wheel.SpringLength = wheelDef.Radius;

            ImportCarParts(wheel.transform.Find("Mesh").gameObject, vtf, wheelDef.Parts, NoColliderPrefab, false);
            wheel.transform.localPosition = vdf.WheelLoc[wheelIndex].Position;

            var wheel2 = Instantiate(wheel, car.transform);
            wheel2.name = placement + "Left";
            wheel2.transform.localPosition = vdf.WheelLoc[wheelIndex + 1].Position;
            wheel2.transform.Find("Mesh").localScale = new Vector3(-1, 1, 1);

            return new[] { wheel, wheel2 };
        }

        private void ImportCarParts(GameObject parent, Vtf vtf, SdfPart[] sdfParts, GameObject prefab, bool justChassis, bool forgetParentPosition = false)
        {
            var partDict = new Dictionary<string, GameObject> { { "WORLD", parent } };

            foreach (var sdfPart in sdfParts)
            {
                if (sdfPart.Name == "NULL")
                    continue;

                if (_bannedNames.Any(b => sdfPart.Name.EndsWith(b)))
                    continue;


                if (justChassis && !(sdfPart.Name.Contains("BDY") || sdfPart.Name.EndsWith("CHAS")))
                    continue;

                var geoFilename = sdfPart.Name + ".geo";
                if(!VirtualFilesystem.Instance.FileExists(geoFilename))
                {
                    Debug.LogWarning("File does not exist: " + geoFilename);
                    continue;
                }

                var partObj = ImportGeo(geoFilename, vtf, prefab);

                var parentName = sdfPart.ParentName;
                if (!partDict.ContainsKey(parentName))
                {
                    Debug.Log("Cant find parent '" + sdfPart.ParentName + "' for '" + sdfPart.Name + "'");
                    parentName = "WORLD";
                }

                if (!forgetParentPosition)
                    partObj.transform.parent = partDict[parentName].transform;
                partObj.transform.right = sdfPart.Right;
                partObj.transform.up = sdfPart.Up;
                partObj.transform.forward = sdfPart.Forward;
                partObj.transform.localPosition = sdfPart.Position;
                if (forgetParentPosition)
                    partObj.transform.parent = partDict[parentName].transform;
                partObj.SetActive(true);
                partDict.Add(sdfPart.Name, partObj);
            }
        }

        public void ClearCache()
        {
            _materialCache.Clear();
            _sdfCache.Clear();
            _meshCache.Clear();
        }
    }
}
