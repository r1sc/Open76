using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets;
using Assets.Fileparsers;
using UnityEngine;
using UnityEngine.Rendering;

public class Game : MonoBehaviour
{
    public string GamePath;
    public Texture2D SurfaceTexture;
    public Material TerrainMaterial;
    public Material SkyMaterial;

    public Terrain[,] TerrainPatches;
    public Vector2 RealTerrainGrid;
    public string MissionFile;

    void Awake()
    {

    }

    // Use this for initialization
    void Start()
    {
        VirtualFilesystem.Instance.Init(GamePath);
        var cacheManager = FindObjectOfType<CacheManager>();

        TerrainPatches = new Terrain[80, 80];
        var mdef = MsnMissionParser.ReadMsnMission(MissionFile);

        cacheManager.Palette = ActPaletteParser.ReadActPalette(mdef.PaletteFilePath);
        SurfaceTexture = TextureParser.ReadMapTexture(mdef.SurfaceTextureFilePath, cacheManager.Palette);

        var skyTexture = TextureParser.ReadMapTexture(mdef.SkyTextureFilePath, cacheManager.Palette);
        SkyMaterial.mainTexture = skyTexture;


        var splatPrototypes = new SplatPrototype[1]
        {
            new SplatPrototype
            {
                texture = SurfaceTexture,
                tileSize = new Vector2(SurfaceTexture.width, SurfaceTexture.height),
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
                patchGameObject.transform.parent = transform;
                //patchGameObject.SetActive(false);

                var terrain = patchGameObject.AddComponent<Terrain>();
                terrain.terrainData = mdef.TerrainPatches[x, z].TerrainData;
                terrain.terrainData.splatPrototypes = splatPrototypes;
                terrain.materialTemplate = TerrainMaterial;
                terrain.materialType = Terrain.MaterialType.Custom;

                var terrainCollider = patchGameObject.AddComponent<TerrainCollider>();
                terrainCollider.terrainData = terrain.terrainData;

                foreach (var odef in mdef.TerrainPatches[x, z].Objects)
                {
                    //Debug.Log("Load " + odef.Label + " id: " + odef.Id);
                    if (odef.ClassId != MsnMissionParser.ClassId.Car && odef.ClassId != MsnMissionParser.ClassId.Special)
                    {
                        cacheManager.ImportSdf(odef.Label + ".sdf", patchGameObject.transform, odef.LocalPosition, odef.LocalRotation);
                    }
                }

                //var texture = new Texture2D(128, 128, TextureFormat.ARGB32, false);
                //for (int iz = 0; iz < 128; iz++)
                //{
                //    for (int ix = 0; ix < 128; ix++)
                //    {
                //        var h = terrain.terrainData.GetHeight(ix, iz);
                //        texture.SetPixel(ix, iz, Color.white * h);
                //    }
                //}
                //texture.Apply();
                //textures.Add(texture);

                TerrainPatches[x, z] = terrain;
                RealTerrainGrid = new Vector2(x, z);
            }
        }


        foreach (var road in mdef.Roads)
        {
            var roadGo = new GameObject("Road");
            roadGo.transform.parent = transform;
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
            var roadMaterial = cacheManager.GetMaterial(roadTextureFilename);
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
            for (int i = 0; i < (vertices.Count - 2)/2; i++)
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
        //foreach (var ldef in mdef.StringObjects)
        //{
        //    var sdfObj = levelManager.ImportSdf(ldef.Label + ".sdf", ldef.Label, null, Vector3.zero, Quaternion.identity);
        //    for (int i = 0; i < ldef.StringPositions.Count; i++)
        //    {
        //        var pos = ldef.StringPositions[i];
        //        Vector3 relSpos;
        //        if (i < ldef.StringPositions.Count - 1)
        //            relSpos = ldef.StringPositions[i + 1];
        //        else
        //            relSpos = ldef.StringPositions[i - 1];

        //        var localPosition = new Vector3(pos.x % 640, pos.y, pos.z % 640);
        //        var patchPosX = (int)(pos.x / 640.0f);
        //        var patchPosZ = (int)(pos.z / 640.0f);
        //        sdfObj.transform.parent = TerrainPatches[patchPosX, patchPosZ].transform;
        //        sdfObj.transform.localPosition = localPosition;
        //        sdfObj.transform.LookAt(relSpos, Vector3.up);
        //        sdfObj = Instantiate(sdfObj);
        //    }
        //}
        //RepositionCurrentTerrainPatch(RealTerrainGrid);
        transform.position = new Vector3(-mdef.Middle.x*640, 0, -mdef.Middle.y*640);
    }

    // Update is called once per frame
    void Update()
    {
        var newx = Camera.main.transform.position.x;
        var newz = Camera.main.transform.position.z;
        bool changed = false;
        var newTerrainGrid = RealTerrainGrid;

        if (newx > 640) // Moved right
        {
            newTerrainGrid.x += 1;
            newx = 0;
            changed = true;
        }
        else if (newx < 0) // Moved left
        {
            newTerrainGrid.x -= 1;
            newx = 640;
            changed = true;
        }
        if (newz > 640) // Moved back
        {
            newTerrainGrid.y += 1;
            newz = 0;
            changed = true;
        }
        else if (newz < 0.0f) // Moved forward
        {
            newTerrainGrid.y -= 1;
            newz = 640;
            changed = true;
        }

        if (changed)
        {
            //Camera.main.transform.position = new Vector3(newx, Camera.main.transform.position.y, newz);
            //RepositionCurrentTerrainPatch(newTerrainGrid);
        }
    }

    private void RepositionCurrentTerrainPatch(Vector2 newTerrainGrid)
    {
        for (int z = -1; z <= 1; z++)
        {
            var tpZ = (int) (RealTerrainGrid.y + z);
            if (tpZ < 0 || tpZ > 79)
                continue;
            for (int x = -1; x <= 1; x++)
            {
                var tpX = (int) (RealTerrainGrid.x + x);
                if (tpX < 0 || tpX > 79)
                    continue;
                var tp = TerrainPatches[tpX, tpZ];
                if (tp == null)
                    continue;
                tp.gameObject.SetActive(false);
            }
        }

        for (int z = -1; z <= 1; z++)
        {
            var tpZ = (int) (newTerrainGrid.y + z);
            if (tpZ < 0 || tpZ > 79)
                continue;
            for (int x = -1; x <= 1; x++)
            {
                var tpX = (int) (newTerrainGrid.x + x);
                if (tpX < 0 || tpX > 79)
                    continue;
                var tp = TerrainPatches[tpX, tpZ];
                if (tp == null)
                    continue;
                tp.gameObject.SetActive(true);
                tp.transform.position = new Vector3(x*640, 0, z*640);
            }
        }

        RealTerrainGrid = newTerrainGrid;
    }
}
