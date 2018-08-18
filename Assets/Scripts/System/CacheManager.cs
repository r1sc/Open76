using System.Collections.Generic;
using System.Linq;
using Assets.Fileparsers;
using UnityEngine;
using Assets.Car;
using System.IO;
using Assets.Scripts.Car;

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
        public Color32[] Palette;
        
        private Material _colorMaterialPrefab;
        private Material _textureMaterialPrefab;
        private Material _transparentMaterialPrefab;
        private Material _carMirrorMaterialPrefab;

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
            _colorMaterialPrefab = Resources.Load<Material>("Materials/AlphaMaterial");
            _textureMaterialPrefab = Resources.Load<Material>("Materials/TextureMaterial");
            _transparentMaterialPrefab = Resources.Load<Material>("Materials/AlphaMaterial");
            _carMirrorMaterialPrefab = Resources.Load<Material>("Materials/CarMirror");

            VirtualFilesystem.Instance.Init(GamePath);
            _materialCache["default"] = Instantiate(_textureMaterialPrefab);
            Palette = ActPaletteParser.ReadActPalette("p02.act");
        }

        public AudioClip GetAudioClip(string soundName)
        {
            string filename = Path.GetFileNameWithoutExtension(soundName);
            if (VirtualFilesystem.Instance.FileExists(filename + ".wav"))
            {
                return VirtualFilesystem.Instance.GetAudioClip(filename + ".wav");
            }

            if (VirtualFilesystem.Instance.FileExists(filename + ".gpw"))
            {
                return GpwParser.ParseGpw(filename + ".gpw").Clip;
            }
            
            Debug.LogWarning("Sound file not found: " + soundName);
            return null;
        }

        public AudioSource GetAudioSource(GameObject rootObject, string soundName)
        {
            const float maxVolume = 0.8f; // Set a default volume since high values cause bad distortion.

            AudioClip audioClip = GetAudioClip(soundName);
            if (audioClip == null)
            {
                return null;
            }

            AudioSource audioSource = rootObject.AddComponent<AudioSource>();
            audioSource.volume = maxVolume;
            audioSource.playOnAwake = false;
            audioSource.clip = audioClip;

            if (audioClip.name.EndsWith(".wav"))
            {
                audioSource.spatialize = false;
                audioSource.spatialBlend = 0.0f;
            }
            else if (audioClip.name.EndsWith(".gpw"))
            {
                audioSource.spatialize = true;
                audioSource.spatialBlend = 1.0f;
                audioSource.minDistance = 5f;
                audioSource.maxDistance = 75f;
            }

            return audioSource;
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
                var material = Instantiate(transparent ? _transparentMaterialPrefab : _textureMaterialPrefab);
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

            var material = Instantiate(_colorMaterialPrefab);
            material.color = color;
            _materialCache[materialName] = material;

            return material;
        }

        private Material GetMaterial(GeoFace geoFace, Vtf vtf, int textureGroup)
        {

            if (geoFace.TextureName != null)
            {
                var textureName = Path.GetFileNameWithoutExtension(geoFace.TextureName);
                if (vtf != null && textureName[0] == 'V')
                {
                    if (textureName.EndsWithFast("BO DY"))
                    {
                        textureName = vtf.Maps[12];
                    }
                    else
                    {
                        var key = textureName.Substring(1).Replace(" ", "").Replace("LF", "LT") + ".TMT";

                        Tmt tmt;
                        if (vtf.Tmts.TryGetValue(key, out tmt))
                        {
                            textureName = tmt.TextureNames[textureGroup];
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

        private GeoMeshCacheEntry ImportMesh(string filename, Vtf vtf, int textureGroup)
        {
            GeoMeshCacheEntry cacheEntry;
            if (_meshCache.TryGetValue(filename, out cacheEntry))
            {
                return cacheEntry;
            }

            var geoMesh = GeoParser.ReadGeoMesh(filename);

            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();
            
            Dictionary<Material, List<GeoFace>> facesGroupedByMaterial = new Dictionary<Material, List<GeoFace>>();
            GeoFace[] faces = geoMesh.Faces;
            for (int i = 0; i < faces.Length; ++i)
            {
                Material material = GetMaterial(faces[i], vtf, textureGroup);

                List<GeoFace> faceGroup;
                if (facesGroupedByMaterial.TryGetValue(material, out faceGroup))
                {
                    faceGroup.Add(faces[i]);
                }
                else
                {
                    facesGroupedByMaterial.Add(material, new List<GeoFace>
                    {
                        faces[i]
                    });
                }
            }

            mesh.subMeshCount = facesGroupedByMaterial.Count;
            var submeshTriangles = new Dictionary<Material, List<int>>();
            foreach (var faceGroup in facesGroupedByMaterial)
            {
                submeshTriangles[faceGroup.Key] = new List<int>();
                List<GeoFace> faceGroupValues = faceGroup.Value;
                foreach (var face in faceGroupValues)
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

            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(normals);
            var subMeshIndex = 0;
            foreach (var submeshTriangle in submeshTriangles)
            {
                mesh.SetTriangles(submeshTriangles[submeshTriangle.Key], subMeshIndex++);
            }
            mesh.RecalculateBounds();

            cacheEntry = new GeoMeshCacheEntry
            {
                GeoMesh = geoMesh,
                Mesh = mesh,
                Materials = facesGroupedByMaterial.Select(x => x.Key).ToArray()
            };
            _meshCache.Add(filename, cacheEntry);

            return cacheEntry;
        }

        public GameObject ImportGeo(string filename, Vtf vtf, GameObject prefab, int textureGroup)
        {
            var meshCacheEntry = ImportMesh(filename, vtf, textureGroup);

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

        private bool TryGetMaskTexture(string filename, out Texture2D maskTexture)
        {
            GeoMeshCacheEntry cacheEntry;
            if (_meshCache.TryGetValue(filename, out cacheEntry))
            {
                foreach (GeoFace face in cacheEntry.GeoMesh.Faces)
                {
                    string textureName = face.TextureName + ".MAP";
                    if (TextureParser.MaskTextureCache.TryGetValue(textureName, out maskTexture))
                    {
                        return true;
                    }
                }
            }

            maskTexture = null;
            return false;
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

                var partObj = ImportGeo(geoFilename, null, _3DObjectPrefab, 0);
                partObj.transform.parent = partDict[sdfPart.ParentName].transform;
                partObj.transform.localPosition = sdfPart.Position;
                partObj.transform.localRotation = Quaternion.identity;
                partObj.SetActive(true);
                partDict.Add(sdfPart.Name, partObj);
            }

            _sdfCache.Add(filename, sdfObject);
            return sdfObject;
        }

        public GameObject ImportVcf(string filename, bool importFirstPerson, out Vdf vdf)
        {
            var vcf = VcfParser.ParseVcf(filename);
            vdf = VdfParser.ParseVdf(vcf.VdfFilename);
            var vtf = VtfParser.ParseVtf(vcf.VtfFilename);

            var carObject = Instantiate(CarPrefab); //ImportGeo(vdf.SOBJGeoName + ".geo", vtf, CarPrefab.gameObject).GetComponent<ArcadeCar>();
            carObject.gameObject.name = vdf.Name + " (" + vcf.VariantName + ")";

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

            Transform[] weaponMountTransforms = new Transform[vdf.HLocs.Count];
            for (int i = 0; i < vdf.HLocs.Count; ++i)
            {
                HLoc hloc = vdf.HLocs[i];
                Transform mountPoint = new GameObject(hloc.Label).transform;
                mountPoint.parent = thirdPerson.transform;
                mountPoint.localRotation = Quaternion.LookRotation(hloc.Forward, hloc.Up);
                mountPoint.localPosition = hloc.Position;
                weaponMountTransforms[i] = mountPoint;
            }

            
            for (int i = 0; i < vdf.PartsThirdPerson.Count; ++i)
            {
                GameObject healthObject = new GameObject("Health " + i);
                healthObject.transform.SetParent(thirdPerson.transform);
                ImportCarParts(healthObject, vtf, vdf.PartsThirdPerson[i], NoColliderPrefab, false, false, i);
                if (i != 0)
                {
                    healthObject.SetActive(false);
                }
            }

            if (importFirstPerson)
            {
                var firstPerson = new GameObject("FirstPerson");
                firstPerson.transform.parent = chassis.transform;
                ImportCarParts(firstPerson, vtf, vdf.PartsFirstPerson, NoColliderPrefab, false, true, 0, LayerMask.NameToLayer("FirstPerson"));

                WeaponsPanel weaponsPanel = firstPerson.AddComponent<WeaponsPanel>();
                weaponsPanel.InitWeapons(vcf);
                
                SystemsPanel systemsPanel = firstPerson.AddComponent<SystemsPanel>();
                systemsPanel.InitSystems();

                firstPerson.SetActive(false);
            }

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
            
            for (int i = 0; i < vcf.Weapons.Count; ++i)
            {
                VcfParser.VcfWeapon weapon = vcf.Weapons[i];
                int mountPoint = weapon.MountPoint;
                HLoc hloc = vdf.HLocs[mountPoint];

                SdfPart[] partsArray;
                switch (hloc.MeshType)
                {
                    case HardpointMeshType.Top:
                        partsArray = weapon.Gdf.TopParts;
                        break;
                    case HardpointMeshType.Side:
                        partsArray = weapon.Gdf.SideParts;
                        break;
                    case HardpointMeshType.Inside:
                        partsArray = weapon.Gdf.InsideParts;
                        break;
                    case HardpointMeshType.Turret:
                        partsArray = weapon.Gdf.TurretParts;
                        break;
                    default:
                        partsArray = null;
                        break;
                }

                if (partsArray != null)
                {
                    Transform weaponTransform = new GameObject(weapon.Gdf.Name).transform;
                    weaponTransform.SetParent(weaponMountTransforms[i]);
                    weaponTransform.localPosition = Vector3.zero;
                    weaponTransform.localRotation = Quaternion.identity;
                    ImportCarParts(weaponTransform.gameObject, vtf, partsArray, NoColliderPrefab, false);

                    // Disable depth test for 'inside' weapons, otherwise they are obscured.
                    if (hloc.MeshType == HardpointMeshType.Inside)
                    {
                        MeshRenderer weaponRenderer = weaponTransform.GetComponentInChildren<MeshRenderer>();
                        if (weaponRenderer != null)
                        {
                            weaponRenderer.sharedMaterial.shader = Shader.Find("Custom/CutOutWithoutZ");
                        }
                    }
                }
            }

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

        private void ImportCarParts(GameObject parent, Vtf vtf, SdfPart[] sdfParts, GameObject prefab, bool justChassis, bool forgetParentPosition = false, int textureGroup = 0, int layerMask = 0)
        {
            var partDict = new Dictionary<string, GameObject> { { "WORLD", parent } };

            foreach (var sdfPart in sdfParts)
            {
                if (sdfPart == null || sdfPart.Name == "NULL")
                    continue;

                if (_bannedNames.Any(b => sdfPart.Name.EndsWithFast(b)))
                    continue;


                if (justChassis && !(sdfPart.Name.Contains("BDY") || sdfPart.Name.EndsWithFast("CHAS")))
                    continue;

                var geoFilename = sdfPart.Name + ".geo";
                if(!VirtualFilesystem.Instance.FileExists(geoFilename))
                {
                    Debug.LogWarning("File does not exist: " + geoFilename);
                    continue;
                }

                var partObj = ImportGeo(geoFilename, vtf, prefab, textureGroup);

                var parentName = sdfPart.ParentName;
                if (!partDict.ContainsKey(parentName))
                {
                    Debug.Log("Cant find parent '" + sdfPart.ParentName + "' for '" + sdfPart.Name + "'");
                    parentName = "WORLD";
                }

                Transform partTransform = partObj.transform;
                if (!forgetParentPosition)
                    partTransform.SetParent(partDict[parentName].transform);
                partTransform.right = sdfPart.Right;
                partTransform.up = sdfPart.Up;
                partTransform.forward = sdfPart.Forward;
                partTransform.localPosition = sdfPart.Position;
                partTransform.localRotation = Quaternion.identity;
                if (forgetParentPosition)
                    partTransform.parent = partDict[parentName].transform;
                partObj.SetActive(true);
                partDict.Add(sdfPart.Name, partObj);

                if (layerMask != 0)
                {
                    partObj.layer = layerMask;
                }

                // Special case for mirrors.
                Texture2D maskTexture;
                if (sdfPart.Name.Contains("MIRI") && TryGetMaskTexture(geoFilename, out maskTexture))
                {
                    RenderTexture renderTexture = new RenderTexture(256, 128, 24);

                    GameObject mirrorCameraObj = Instantiate(partObj);
                    Transform mirrorObjTransform = mirrorCameraObj.transform;
                    mirrorObjTransform.SetParent(partObj.transform);
                    mirrorObjTransform.localPosition = Vector3.zero;
                    mirrorObjTransform.localRotation = Quaternion.identity;

                    Material mirrorMaterial = Instantiate(_carMirrorMaterialPrefab);
                    MeshRenderer meshRenderer = partObj.transform.GetComponent<MeshRenderer>();
                    mirrorMaterial.mainTexture = meshRenderer.material.mainTexture;
                    mirrorMaterial.SetTexture("_MaskTex", maskTexture);
                    meshRenderer.material = mirrorMaterial;

                    GameObject cameraPivotObj = new GameObject("Camera Pivot");
                    Camera mirrorCamera = cameraPivotObj.AddComponent<Camera>();
                    mirrorCamera.cullingMask = ~LayerMask.GetMask("FirstPerson");
                    mirrorCamera.targetTexture = renderTexture;
                    Transform pivotTransform = cameraPivotObj.transform;
                    pivotTransform.SetParent(mirrorObjTransform);
                    pivotTransform.localPosition = Vector3.zero;
                    pivotTransform.localRotation = Quaternion.Euler(0f, 180f, 0f);

                    Material cameraMaterial = Instantiate(_carMirrorMaterialPrefab);
                    cameraMaterial.mainTexture = renderTexture;
                    MeshRenderer mirrorRenderer = mirrorCameraObj.GetComponent<MeshRenderer>();
                    cameraMaterial.SetTexture("_MaskTex", maskTexture);
                    mirrorRenderer.material = cameraMaterial;
                }
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
