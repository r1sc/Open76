using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Fileparsers
{
    public class MsnMissionParser
    {
        //public class TerrainData
        //{
        //    public void SetHeights(int x, int y, float[,] heights) { }
        //}

        public class Odef
        {
            public string Label { get; set; }
            public int Id { get; set; }
            public Vector3 LocalPosition { get; set; }
            public uint ClassId { get; set; }
            public uint TeamId { get; set; }
            public Quaternion LocalRotation { get; set; }
        }

        public class TerrainPatch
        {
            public TerrainData TerrainData { get; set; }
            public List<Odef> Objects { get; set; }

            public TerrainPatch(TerrainData terrainData)
            {
                TerrainData = terrainData;
                Objects = new List<Odef>();
            }
        }

        public class MissonDefinition
        {
            public TerrainPatch[,] TerrainPatches { get; private set; }
            
            public string PaletteFilePath { get; set; }
            public string LumaTableFilePath { get; set; }
            public string XlucencyTableFilePath { get; set; }
            public string ObjectiveFilePath { get; set; }
            public string SkyTextureFilePath { get; set; }
            public string ScroungeTextureFilePath { get; set; }
            public string SurfaceTextureFilePath { get; set; }
            public string LevelMapFilePath { get; set; }
            public string HzdFilePath { get; set; }

            public MissonDefinition()
            {
                TerrainPatches = new TerrainPatch[80, 80];
            }
        }

        public static MissonDefinition ReadMsnMission(string filename)
        {
            var mdef = new MissonDefinition();
            using (var msn = new Bwd2Reader(filename))
            {
                msn.FindNext("WDEF");
                using (var wdef = new Bwd2Reader(msn))
                {
                    wdef.FindNext("WRLD");
                    wdef.BaseStream.Seek(30, SeekOrigin.Current);
                    mdef.PaletteFilePath = new string(wdef.ReadChars(13)).Replace("\0", "");
                    mdef.LumaTableFilePath = new string(wdef.ReadChars(13)).Replace("\0", "");
                    mdef.XlucencyTableFilePath = new string(wdef.ReadChars(13)).Replace("\0", "");
                    mdef.ObjectiveFilePath = new string(wdef.ReadChars(13)).Replace("\0", "");
                    mdef.SkyTextureFilePath = new string(wdef.ReadChars(13)).Replace("\0", "");
                    mdef.ScroungeTextureFilePath = new string(wdef.ReadChars(13)).Replace("\0", "");
                    mdef.SurfaceTextureFilePath = new string(wdef.ReadChars(13)).Replace("\0", "");
                    mdef.LevelMapFilePath = new string(wdef.ReadChars(13)).Replace("\0", "");
                    mdef.HzdFilePath = new string(wdef.ReadChars(13)).Replace("\0", "");
                }
                msn.FindNext("TDEF");
                using (var tdef = new Bwd2Reader(msn))
                {
                    tdef.FindNext("ZMAP");
                    var numUniqueTerrainPatches = tdef.ReadByte();
                    var patchConfig = tdef.ReadBytes(80 * 80);

                    tdef.FindNext("ZONE");
                    var unk = tdef.ReadByte();
                    var terrainFilePath = new string(tdef.ReadChars(13)).Replace("\0", "");

                    var terrainDatas = new List<TerrainData>();
                    using (var terr = new BinaryReader(VirtualFilesystem.Instance.GetFileStream(terrainFilePath)))
                    {
                        while (terr.BaseStream.Position < terr.BaseStream.Length)
                        {
                            var heights = new float[129, 129];
                            for (int z = 0; z < 128; z++)
                            {
                                for (int x = 0; x < 128; x++)
                                {
                                    var tpoint = terr.ReadUInt16();
                                    var height = (tpoint & 0x0FFF) / 4095.0f;
                                    heights[z, x] = height;
                                }
                            }

                            var tdata = CreateTerrainData();
                            tdata.size = new Vector3(640, 407, 640);
                            tdata.SetHeights(0, 0, heights);
                            terrainDatas.Add(tdata);
                        }
                    }

                    var defaultTerrainData = CreateTerrainData();
                    for (int z = 0; z < 80; z++)
                    {
                        for (int x = 0; x < 80; x++)
                        {
                            var patchIdx = patchConfig[z * 80 + x];
                            if (patchIdx == 0xFF)
                            {
                                //mdef.TerrainPatches[x, z] = new TerrainPatch(defaultTerrainData);
                            }
                            else
                            {
                                mdef.TerrainPatches[x, z] = new TerrainPatch(terrainDatas[patchIdx]);
                            }
                        }
                    }
                }
                msn.FindNext("ODEF");
                using (var odef = new Bwd2Reader(msn))
                {
                    odef.FindNext("OBJ");
                    while (odef.Current.Name != "EXIT")
                    {
                        var rawlabel = odef.ReadBytes(8);
                        int labelhigh = 0;
                        StringBuilder labelBuilder = new StringBuilder();
                        for (int i = 0; i < 8; i++)
                        {
                            var v = rawlabel[i];
                            if (v > 0x7f)
                            {
                                labelhigh = (labelhigh << 1) | 0x01;
                            }
                            else
                            {
                                labelhigh = (labelhigh << 1) & 0xfe;
                            }
                            v = (byte) (v & 0x7f);
                            if (v != 0)
                                labelBuilder.Append((char)v);
                        }
                        var label = labelBuilder.ToString();
                        var vec1 = new Vector3(odef.ReadSingle(), odef.ReadSingle(), odef.ReadSingle());
                        var upwards = new Vector3(odef.ReadSingle(), odef.ReadSingle(), odef.ReadSingle());
                        var forward = new Vector3(odef.ReadSingle(), odef.ReadSingle(), odef.ReadSingle());
                        var pos = new Vector3(odef.ReadSingle(), odef.ReadSingle(), odef.ReadSingle());
                        odef.BaseStream.Position += 36;
                        var classId = odef.ReadUInt32();
                        var teamId = odef.ReadUInt32();

                        var localPosition = new Vector3(pos.x%640, pos.y, pos.z%640);
                        var patchPosX = (int)(pos.x/640.0f);
                        var patchPosZ = (int)(pos.z/640.0f);
                        mdef.TerrainPatches[patchPosX, patchPosZ].Objects.Add(new Odef
                        {
                            Label = label,
                            Id = labelhigh,
                            LocalPosition = localPosition,
                            ClassId = classId,
                            TeamId = teamId,
                            LocalRotation = Quaternion.LookRotation(forward, upwards)
                        });

                        odef.Next();
                    }
                }
            }
            return mdef;
        }

        private static TerrainData CreateTerrainData()
        {
            var tdata = new TerrainData();
            tdata.heightmapResolution = 128;
            tdata.size = new Vector3(640, 100, 640);
            return tdata;
        }

        //private static TerrainData ReadTerTerrain(string terPath)
        //{

        //}
    }
}
