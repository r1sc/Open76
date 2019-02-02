using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Camera;
using Assets.Scripts.CarSystems;
using Assets.Scripts.Entities;
using Assets.Scripts.System.Fileparsers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.System
{
    public class LevelLoader
    {
        private readonly Material _terrainMaterial;
        private readonly GameObject _spawnPrefab;
        private readonly GameObject _regenPrefab;
        private readonly Scene _levelScene;
        private readonly CacheManager _cacheManager;
        private readonly Game _game;

        private static LevelLoader _instance;

        public static LevelLoader Instance
        {
            get { return _instance ?? (_instance = new LevelLoader()); }
        }

        private LevelLoader()
        {
            _terrainMaterial = Resources.Load<Material>("Materials/Terrain");
            _spawnPrefab = Resources.Load<GameObject>("Prefabs/SpawnPrefab");
            _regenPrefab = Resources.Load<GameObject>("Prefabs/RegenPrefab");
            _cacheManager = CacheManager.Instance;
            _game = Game.Instance;
        }
        
        public IEnumerator LoadLevel(string msnFilename)
        {
            _game.LevelName = msnFilename;
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName != "Level")
            {
                AsyncOperation sceneLoad = SceneManager.LoadSceneAsync(1, LoadSceneMode.Single);
                sceneLoad.allowSceneActivation = true;
                while (!sceneLoad.isDone)
                {
                    yield return null;
                }

                yield break;
            }

            Terrain[,] terrainPatches = new Terrain[80, 80];
            MsnMissionParser.MissonDefinition mdef = MsnMissionParser.ReadMsnMission(msnFilename);

            _cacheManager.Palette = ActPaletteParser.ReadActPalette(mdef.PaletteFilePath);
            Texture2D surfaceTexture = TextureParser.ReadMapTexture(mdef.SurfaceTextureFilePath, _cacheManager.Palette, TextureFormat.RGB24, true, FilterMode.Point);
            GameObject.Find("Sky").GetComponent<Sky>().TextureFilename = mdef.SkyTextureFilePath;

            GameObject worldGameObject = GameObject.Find("World");
            if (worldGameObject != null)
            {
                Object.Destroy(worldGameObject);
            }

            worldGameObject = new GameObject("World");

            TerrainLayer[] terrainLayers = new[]
            {
                new TerrainLayer
                {
                    diffuseTexture = surfaceTexture,
                    tileSize = new Vector2(surfaceTexture.width, surfaceTexture.height) / 10.0f,
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

                    GameObject patchGameObject = new GameObject("Ter " + x + ", " + z);
                    patchGameObject.layer = LayerMask.NameToLayer("Terrain");
                    patchGameObject.transform.position = new Vector3(x * 640, 0, z * 640);
                    patchGameObject.transform.parent = worldGameObject.transform;

                    Terrain terrain = patchGameObject.AddComponent<Terrain>();
                    terrain.terrainData = mdef.TerrainPatches[x, z].TerrainData;
                    terrain.terrainData.terrainLayers = terrainLayers;
                    terrain.materialTemplate = _terrainMaterial;
                    terrain.materialType = Terrain.MaterialType.Custom;

                    TerrainCollider terrainCollider = patchGameObject.AddComponent<TerrainCollider>();
                    terrainCollider.terrainData = terrain.terrainData;

                    foreach (MsnMissionParser.Odef odef in mdef.TerrainPatches[x, z].Objects)
                    {
                        GameObject go = null;
                        if (odef.ClassId == MsnMissionParser.ClassId.Car)
                        {
                            string lblUpper = odef.Label.ToUpper();
                            
                            // Training mission uses hardcoded VCF for player.
                            string vcfName = odef.Label;
                            if (msnFilename.ToLower() == "a01.msn" && vcfName == "vppirna1")
                            {
                                vcfName = "vppa01";
                            }

                            switch (lblUpper)
                            {
                                case "SPAWN":
                                    go = Object.Instantiate(_spawnPrefab);
                                    go.tag = "Spawn";
                                    break;
                                case "REGEN":
                                    go = Object.Instantiate(_regenPrefab);
                                    go.tag = "Regen";
                                    break;
                                default:
                                    go = _cacheManager.ImportVcf(vcfName + ".vcf", odef.IsPlayer, out _);
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
                            bool canWreck = odef.ClassId == MsnMissionParser.ClassId.Struct1 ||
                                            odef.ClassId == MsnMissionParser.ClassId.Ramp ||
                                            odef.ClassId == MsnMissionParser.ClassId.Struct2;

                            go = _cacheManager.ImportSdf(odef.Label + ".sdf", patchGameObject.transform, odef.LocalPosition, odef.LocalRotation, canWreck, out Sdf sdf, out GameObject wreckedPart);
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
            foreach (MsnMissionParser.Road road in mdef.Roads)
            {
                roadManager.CreateRoadObject(road, mdef.Middle * 640);
            }

            foreach (MsnMissionParser.Ldef ldef in mdef.StringObjects)
            {
                GameObject sdfObj = _cacheManager.ImportSdf(ldef.Label + ".sdf", null, Vector3.zero, Quaternion.identity, false, out _, out _);

                for (int i = 0; i < ldef.StringPositions.Length; i++)
                {
                    Vector3 pos = ldef.StringPositions[i];
                    Vector3 localPosition = new Vector3(pos.x % 640, pos.y, pos.z % 640);
                    int patchPosX = (int)(pos.x / 640.0f);
                    int patchPosZ = (int)(pos.z / 640.0f);
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
                        sdfObj = Object.Instantiate(sdfObj);
                }
            }

            worldGameObject.transform.position = new Vector3(-mdef.Middle.x * 640, 0, -mdef.Middle.y * 640);
            
            Object.FindObjectOfType<Light>().color = _cacheManager.Palette[176];
            UnityEngine.Camera.main.backgroundColor = _cacheManager.Palette[239];
            RenderSettings.fogColor = _cacheManager.Palette[239];
            RenderSettings.ambientLight = _cacheManager.Palette[247];

            List<Car> cars = EntityManager.Instance.Cars;
            foreach (Car car in cars)
            {
                car.transform.parent = null;
            }

            if (mdef.FSM != null)
            {
                foreach (StackMachine machine in mdef.FSM.StackMachines)
                {
                    machine.Reset();
                    machine.Constants = new IntRef[machine.InitialArguments.Length];
                    for (int i = 0; i < machine.Constants.Length; i++)
                    {
                        int stackValue = machine.InitialArguments[i];
                        machine.Constants[i] = mdef.FSM.Constants[stackValue];
                    }
                }

                FSMRunner fsmRunner = FSMRunner.Instance;
                fsmRunner.FSM = mdef.FSM;
            }
        }
    }
}