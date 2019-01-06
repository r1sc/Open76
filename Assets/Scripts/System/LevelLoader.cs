using Assets.Fileparsers;
using Assets.Scripts.System;
using System.Collections.Generic;
using Assets.Scripts.Camera;
using Assets.Scripts.CarSystems;
using Assets.Scripts.Entities;
using UnityEngine;

namespace Assets.System
{
    class LevelLoader : MonoBehaviour
    {
        public Dictionary<int, GameObject> LevelObjects;
        private Material _terrainMaterial;
        private GameObject _spawnPrefab;
        private GameObject _regenPrefab;

        private void Awake()
        {
            _terrainMaterial = Resources.Load<Material>("Materials/Terrain");
            _spawnPrefab = Resources.Load<GameObject>("Prefabs/SpawnPrefab");
            _regenPrefab = Resources.Load<GameObject>("Prefabs/RegenPrefab");
        }

        public void LoadLevel(string msnFilename)
        {
            LevelObjects = new Dictionary<int, GameObject>();
            var cacheManager = CacheManager.Instance;

            var terrainPatches = new Terrain[80, 80];
            var mdef = MsnMissionParser.ReadMsnMission(msnFilename);

            cacheManager.Palette = ActPaletteParser.ReadActPalette(mdef.PaletteFilePath);
            var _surfaceTexture = TextureParser.ReadMapTexture(mdef.SurfaceTextureFilePath, cacheManager.Palette, TextureFormat.RGB24, true, FilterMode.Point);

            FindObjectOfType<Sky>().TextureFilename = mdef.SkyTextureFilePath;

            var worldGameObject = GameObject.Find("World");
            if (worldGameObject != null)
                Destroy(worldGameObject);
            worldGameObject = new GameObject("World");


            var terrainLayers = new[]
            {
                new TerrainLayer
                {
                    diffuseTexture = _surfaceTexture,
                    tileSize = new Vector2(_surfaceTexture.width, _surfaceTexture.height) / 10.0f,
                    metallic = 0,
                    smoothness = 0
                }
            };
            
            for (int z = 0; z < 80; z++)
            {
                for (int x = 0; x < 80; x++)
                {
                    if (mdef.TerrainPatches[x, z] == null)
                        continue;

                    var patchGameObject = new GameObject("Ter " + x + ", " + z);
                    patchGameObject.layer = LayerMask.NameToLayer("Terrain");
                    patchGameObject.transform.position = new Vector3(x * 640, 0, z * 640);
                    patchGameObject.transform.parent = worldGameObject.transform;

                    var terrain = patchGameObject.AddComponent<Terrain>();
                    terrain.terrainData = mdef.TerrainPatches[x, z].TerrainData;
                    terrain.terrainData.terrainLayers = terrainLayers;
                    terrain.materialTemplate = _terrainMaterial;
                    terrain.materialType = Terrain.MaterialType.Custom;

                    var terrainCollider = patchGameObject.AddComponent<TerrainCollider>();
                    terrainCollider.terrainData = terrain.terrainData;

                    foreach (var odef in mdef.TerrainPatches[x, z].Objects)
                    {
                        GameObject go = null;
                        if (odef.ClassId == MsnMissionParser.ClassId.Car)
                        {
                            var lblUpper = odef.Label.ToUpper();
                            
                            // Training mission uses hardcoded VCF for player.
                            string vcfName = odef.Label;
                            if (msnFilename.ToLower() == "a01.msn" && vcfName == "vppirna1")
                            {
                                vcfName = "vppa01";
                            }

                            switch (lblUpper)
                            {
                                case "SPAWN":
                                    go = Instantiate(_spawnPrefab);
                                    go.tag = "Spawn";
                                    break;
                                case "REGEN":
                                    go = Instantiate(_regenPrefab);
                                    go.tag = "Regen";
                                    break;
                                default:
                                    Vdf vdf;
                                    go = cacheManager.ImportVcf(vcfName + ".vcf", odef.IsPlayer, out vdf);
                                    Car car = go.GetComponent<Car>();
                                    car.TeamId = odef.TeamId;
                                    car.IsPlayer = odef.IsPlayer;
                                    break;
                            }

                            go.transform.parent = patchGameObject.transform;
                            go.transform.localPosition = odef.LocalPosition;
                            go.transform.localRotation = odef.LocalRotation;

                            if (odef.IsPlayer)
                            {
                                CameraManager.Instance.MainCamera.GetComponent<SmoothFollow>().Target = go.transform;
                                go.AddComponent<CarInput>();
                            }
                        }
                        else if (odef.ClassId != MsnMissionParser.ClassId.Special)
                        {
                            Sdf sdf;
                            GameObject wreckedPart;
                            bool canWreck = odef.ClassId == MsnMissionParser.ClassId.Struct1 ||
                                            odef.ClassId == MsnMissionParser.ClassId.Ramp ||
                                            odef.ClassId == MsnMissionParser.ClassId.Struct2;

                            go = cacheManager.ImportSdf(odef.Label + ".sdf", patchGameObject.transform, odef.LocalPosition, odef.LocalRotation, canWreck, out sdf, out wreckedPart);
                            if (odef.ClassId == MsnMissionParser.ClassId.Sign)
                            {
                                go.AddComponent<Sign>();
                            }
                            else if (canWreck)
                            {
                                Building building = go.AddComponent<Building>();
                                building.Initialise(sdf, wreckedPart);
                            }
                        }

                        if(go != null)
                        {
                            go.name = odef.Label + "_" + odef.Id;
                            LevelObjects.Add(go.GetInstanceID(), go);

                            if (mdef.FSM != null)
                            {
                                FSMEntity[] entities = mdef.FSM.EntityTable;
                                for (int i = 0; i < entities.Length; ++i)
                                {
                                    if (entities[i].Value == odef.Label && entities[i].Id == odef.Id)
                                    {
                                        WorldEntity worldEntity = go.GetComponent<WorldEntity>();
                                        if (worldEntity != null)
                                        {
                                            entities[i].WorldEntity = worldEntity;
                                            worldEntity.Id = i;
                                        }
                                        
                                        entities[i].Object = go;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    terrainPatches[x, z] = terrain;
                }
            }

            RoadManager roadManager = RoadManager.Instance;
            foreach (var road in mdef.Roads)
            {
                roadManager.CreateRoadObject(road, mdef.Middle * 640);
            }

            foreach (var ldef in mdef.StringObjects)
            {
                var sdfObj = cacheManager.ImportSdf(ldef.Label + ".sdf", null, Vector3.zero, Quaternion.identity, false, out _, out _);

                for (int i = 0; i < ldef.StringPositions.Length; i++)
                {
                    var pos = ldef.StringPositions[i];
                    var localPosition = new Vector3(pos.x % 640, pos.y, pos.z % 640);
                    var patchPosX = (int)(pos.x / 640.0f);
                    var patchPosZ = (int)(pos.z / 640.0f);
                    sdfObj.name = ldef.Label + " " + i;
                    sdfObj.transform.parent = terrainPatches[patchPosX, patchPosZ].transform;
                    sdfObj.transform.localPosition = localPosition;
                    if (i < ldef.StringPositions.Length - 1)
                    {
                        sdfObj.transform.LookAt(ldef.StringPositions[i + 1], Vector3.up);
                    }
                    else
                    {
                        sdfObj.transform.LookAt(ldef.StringPositions[i - 1], Vector3.up);
                        sdfObj.transform.localRotation *= Quaternion.AngleAxis(180, Vector3.up);
                    }

                    if (i < ldef.StringPositions.Length - 1)
                        sdfObj = Instantiate(sdfObj);
                }
            }

            worldGameObject.transform.position = new Vector3(-mdef.Middle.x * 640, 0, -mdef.Middle.y * 640);


            FindObjectOfType<Light>().color = cacheManager.Palette[176];
            Camera.main.backgroundColor = cacheManager.Palette[239];
            RenderSettings.fogColor = cacheManager.Palette[239];
            RenderSettings.ambientLight = cacheManager.Palette[247];

            var cars = EntityManager.Instance.Cars;
            foreach (var car in cars)
            {
                car.transform.parent = null;
            }

            if (mdef.FSM != null)
            {
                foreach (var machine in mdef.FSM.StackMachines)
                {
                    machine.Reset();
                    machine.Constants = new IntRef[machine.InitialArguments.Length];
                    for (int i = 0; i < machine.Constants.Length; i++)
                    {
                        var stackValue = machine.InitialArguments[i];
                        machine.Constants[i] = mdef.FSM.Constants[stackValue];
                    }
                }

                var fsmRunner = FSMRunner.Instance;
                fsmRunner.FSM = mdef.FSM;
            }
        }
    }
}