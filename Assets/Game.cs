using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets;
using Assets.Fileparsers;
using UnityEngine;

public class Game : MonoBehaviour
{
    public string GamePath;
    public string Path;
    public string PalettePath;
    public Texture2D[] Textures;
    public Texture2D SurfaceTexture;
    public Material TerrainMaterial;
    public Material SkyMaterial;
    public Material TextureMaterialPrefab;

    public Terrain[,] TerrainPatches;
    public Vector2 RealTerrainGrid;
    
    public Color32[] Palette;

    public string SdfToLoad;

    void Awake()
    {

    }

    // Use this for initialization
    void Start()
    {
        VirtualFilesystem.Instance.Init(GamePath);

        Palette = ActPaletteParser.ReadActPalette("t01.act");
        var levelManager = new LevelManager();
        levelManager.TextureMaterialPrefab = TextureMaterialPrefab;
        levelManager.Palette = Palette;

        levelManager.ImportSdf(SdfToLoad);
        

        TerrainPatches = new Terrain[80, 80];
        var textures = new List<Texture2D>();
        var mdef = MsnMissionParser.ReadMsnMission("T01.msn");

        var palette = ActPaletteParser.ReadActPalette(mdef.PaletteFilePath);
        SurfaceTexture = MapTextureParser.ReadMapTexture(mdef.SurfaceTextureFilePath, palette);

        var skyTexture = MapTextureParser.ReadMapTexture(mdef.SkyTextureFilePath, palette);
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
                var terrainGo = new GameObject("Ter " + x + ", " + z);
                terrainGo.SetActive(false);
                var terrain = terrainGo.AddComponent<Terrain>();
                //terrainGo.transform.parent = transform;
                //terrain.enabled = false;
                terrain.terrainData = mdef.TerrainPatches[x, z];
                terrain.terrainData.splatPrototypes = splatPrototypes;
                terrain.materialTemplate = TerrainMaterial;
                terrain.materialType = Terrain.MaterialType.Custom;

                terrainGo.transform.position = new Vector3(x * 640, 0, z * 640);
                var terrainCo = terrainGo.AddComponent<TerrainCollider>();
                terrainCo.terrainData = terrain.terrainData;

                var texture = new Texture2D(128, 128, TextureFormat.ARGB32, false);
                for (int iz = 0; iz < 128; iz++)
                {
                    for (int ix = 0; ix < 128; ix++)
                    {
                        var h = terrain.terrainData.GetHeight(ix, iz);
                        texture.SetPixel(ix, iz, Color.white * h);
                    }
                }
                texture.Apply();
                textures.Add(texture);

                TerrainPatches[x, z] = terrain;
            }
        }
        Textures = textures.ToArray();

        RepositionCurrentTerrainPatch(RealTerrainGrid);
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
            Camera.main.transform.position = new Vector3(newx, Camera.main.transform.position.y, newz);
            RepositionCurrentTerrainPatch(newTerrainGrid);
        }
    }

    private void RepositionCurrentTerrainPatch(Vector2 newTerrainGrid)
    {
        for (int z = -1; z <= 1; z++)
        {
            var tpZ = (int)(RealTerrainGrid.y + z);
            if (tpZ < 0 || tpZ > 79)
                continue;
            for (int x = -1; x <= 1; x++)
            {
                var tpX = (int)(RealTerrainGrid.x + x);
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
            var tpZ = (int)(newTerrainGrid.y + z);
            if (tpZ < 0 || tpZ > 79)
                continue;
            for (int x = -1; x <= 1; x++)
            {
                var tpX = (int)(newTerrainGrid.x + x);
                if (tpX < 0 || tpX > 79)
                    continue;
                var tp = TerrainPatches[tpX, tpZ];
                if (tp == null)
                    continue;
                tp.gameObject.SetActive(true);
                tp.transform.position = new Vector3(x * 640, 0, z * 640);
            }
        }

        RealTerrainGrid = newTerrainGrid;
    }
}
