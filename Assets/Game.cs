
using System;
using System.Collections.Generic;
using Assets;
using Assets.Fileparsers;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

public class Game : MonoBehaviour
{
    public Material TerrainMaterial;
    public GameObject SpawnPrefab, RegenPrefab;

    public string MissionFile;
    public string VcfToLoad;

    // Use this for initialization
    void Start()
    {
        LoadLevel(MissionFile);
    }

    void LoadLevel(string msnFilename)
    {
        var cacheManager = FindObjectOfType<CacheManager>();

        var terrainPatches = new Terrain[80, 80];
        var mdef = MsnMissionParser.ReadMsnMission(msnFilename);

        cacheManager.Palette = ActPaletteParser.ReadActPalette(mdef.PaletteFilePath);
        var _surfaceTexture = TextureParser.ReadMapTexture(mdef.SurfaceTextureFilePath, cacheManager.Palette);
        _surfaceTexture.filterMode = FilterMode.Point;

        FindObjectOfType<Sky>().TextureFilename = mdef.SkyTextureFilePath;
        // var skyTexture = TextureParser.ReadMapTexture(mdef.SkyTextureFilePath, cacheManager.Palette);
        // SkyMaterial.mainTexture = skyTexture;

        var worldGameObject = GameObject.Find("World");
        if (worldGameObject != null)
            Destroy(worldGameObject);
        worldGameObject = new GameObject("World");


        var splatPrototypes = new[]
        {
            new SplatPrototype
            {
                texture = _surfaceTexture,
                tileSize = new Vector2(_surfaceTexture.width, _surfaceTexture.height),
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
                patchGameObject.transform.position = new Vector3(x * 640, 0, z * 640);
                patchGameObject.transform.parent = worldGameObject.transform;

                var terrain = patchGameObject.AddComponent<Terrain>();
                terrain.terrainData = mdef.TerrainPatches[x, z].TerrainData;
                terrain.terrainData.splatPrototypes = splatPrototypes;
                terrain.materialTemplate = TerrainMaterial;
                terrain.materialType = Terrain.MaterialType.Custom;

                var terrainCollider = patchGameObject.AddComponent<TerrainCollider>();
                terrainCollider.terrainData = terrain.terrainData;

                foreach (var odef in mdef.TerrainPatches[x, z].Objects)
                {
                    if (odef.ClassId == MsnMissionParser.ClassId.Car)
                    {
                        var lblUpper = odef.Label.ToUpper();

                        GameObject carObj;
                        switch (lblUpper)
                        {
                            case "SPAWN":
                                carObj = Instantiate(SpawnPrefab);
                                carObj.tag = "Spawn";
                                break;
                            case "REGEN":
                                carObj = Instantiate(RegenPrefab);
                                carObj.tag = "Regen";
                                break;
                            default:
                                carObj = cacheManager.ImportVcf(odef.Label + ".vcf");
                                break;
                        }
                        carObj.transform.parent = patchGameObject.transform;
                        carObj.transform.localPosition = odef.LocalPosition;
                        carObj.transform.localRotation = odef.LocalRotation;
                    }
                    else if (odef.ClassId != MsnMissionParser.ClassId.Special)
                    {
                        var go = cacheManager.ImportSdf(odef.Label + ".sdf", patchGameObject.transform, odef.LocalPosition, odef.LocalRotation);
                        if(odef.ClassId == MsnMissionParser.ClassId.Sign)
                        {                            
                            go.AddComponent<FlyOffOnImpact>();
                        }
                    }
                }

                terrainPatches[x, z] = terrain;
            }
        }

        foreach (var road in mdef.Roads)
        {
            var roadGo = new GameObject("Road");
            roadGo.transform.parent = worldGameObject.transform;
            var meshFilter = roadGo.AddComponent<MeshFilter>();
            var meshRenderer = roadGo.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;


            string roadTextureFilename;
            switch (road.SegmentType)
            {
                case MsnMissionParser.RoadSegmentType.PavedHighway:
                    roadTextureFilename = "r2ayr_51";
                    break;
                case MsnMissionParser.RoadSegmentType.DirtTrack:
                    roadTextureFilename = "r2dnr_37";
                    break;
                case MsnMissionParser.RoadSegmentType.RiverBed:
                    roadTextureFilename = "r2wnr_39";
                    break;
                case MsnMissionParser.RoadSegmentType.FourLaneHighway:
                    roadTextureFilename = "r2ayr_51";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            var roadMaterial = cacheManager.GetTextureMaterial(roadTextureFilename, false);
            meshRenderer.material = roadMaterial;

            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();

            var uvIdx = 0;
            foreach (var roadSegment in road.RoadSegments)
            {
                vertices.Add(roadSegment.Left);
                vertices.Add(roadSegment.Right);
                uvs.Add(new Vector2(0, uvIdx));
                uvs.Add(new Vector2(1, uvIdx));
                uvIdx += 1;
            }

            var indices = new List<int>();
            var idx = 0;
            for (int i = 0; i < (vertices.Count - 2) / 2; i++)
            {
                indices.Add(idx + 2);
                indices.Add(idx + 1);
                indices.Add(idx);

                indices.Add(idx + 2);
                indices.Add(idx + 3);
                indices.Add(idx + 1);
                idx += 2;
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = indices.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            meshFilter.sharedMesh = mesh;
        }

        foreach (var ldef in mdef.StringObjects)
        {
            var sdfObj = cacheManager.ImportSdf(ldef.Label + ".sdf", null, Vector3.zero, Quaternion.identity);

            for (int i = 0; i < ldef.StringPositions.Count; i++)
            {
                var pos = ldef.StringPositions[i];
                var localPosition = new Vector3(pos.x % 640, pos.y, pos.z % 640);
                var patchPosX = (int)(pos.x / 640.0f);
                var patchPosZ = (int)(pos.z / 640.0f);
                sdfObj.name = ldef.Label + " " + i;
                sdfObj.transform.parent = terrainPatches[patchPosX, patchPosZ].transform;
                sdfObj.transform.localPosition = localPosition;
                if (i < ldef.StringPositions.Count - 1)
                {
                    sdfObj.transform.LookAt(ldef.StringPositions[i + 1], Vector3.up);
                }
                else
                {
                    sdfObj.transform.LookAt(ldef.StringPositions[i - 1], Vector3.up);
                    sdfObj.transform.localRotation *= Quaternion.AngleAxis(180, Vector3.up);
                }

                if (i < ldef.StringPositions.Count - 1)
                    sdfObj = Instantiate(sdfObj);
            }
        }

        worldGameObject.transform.position = new Vector3(-mdef.Middle.x * 640, 0, -mdef.Middle.y * 640);


        FindObjectOfType<Light>().color = cacheManager.Palette[176];
        Camera.main.backgroundColor = cacheManager.Palette[239];
        RenderSettings.fogColor = cacheManager.Palette[239];
        RenderSettings.ambientLight = cacheManager.Palette[247];

        if (MissionFile.ToLower().StartsWith("m"))
        {
            var importedVcf = cacheManager.ImportVcf(VcfToLoad);
            importedVcf.AddComponent<InputCarController>();

            var spawnPoint = GameObject.FindGameObjectsWithTag("Spawn")[0];
            importedVcf.transform.position = spawnPoint.transform.position;
            importedVcf.transform.rotation = spawnPoint.transform.rotation;

            Camera.main.GetComponent<SmoothFollow>().Target = importedVcf.transform;
        }
    }


    // Update is called once per frame
    void Update()
    {

    }
}
