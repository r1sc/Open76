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

        public enum ClassId : uint
        {
            Car = 1,
            Struct1 = 2,
            Struct2 = 3,
            Sign = 4,
            Special = 9,
            Ramp = 11,
            Intersection = 80
        }

        public class Odef
        {
            public string Label { get; set; }
            public int Id { get; set; }
            public Vector3 LocalPosition { get; set; }
            public ClassId ClassId { get; set; }
            public ushort TeamId { get; set; }
            public Quaternion LocalRotation { get; set; }
        }

        public class Ldef
        {
            public string Label { get; set; }
            public List<Vector3> StringPositions { get; set; }
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

        public class RoadSegment
        {
            public Vector3 Left { get; set; }
            public Vector3 Right { get; set; }
            public RoadSegment Previous { get; set; }
        }

        public enum RoadSegmentType : uint
        {
            PavedHighway = 0,
            DirtTrack = 1,
            RiverBed = 2,
            FourLaneHighway = 3072
        }

        public class Road
        {
            public List<RoadSegment> RoadSegments { get; set; }
            public RoadSegmentType SegmentType { get; set; }

            public Road()
            {
                RoadSegments = new List<RoadSegment>();
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
            public List<Ldef> StringObjects { get; set; }

            public MissonDefinition()
            {
                TerrainPatches = new TerrainPatch[80, 80];
                StringObjects = new List<Ldef>();
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

                    var heights = new List<float[,]>();
                    using (var terr = new BinaryReader(VirtualFilesystem.Instance.GetFileStream(terrainFilePath)))
                    {
                        for (var i = 0; i < numUniqueTerrainPatches; i++)
                        {
                            var h = new float[129, 129];
                            for (int z = 0; z < 128; z++)
                            {
                                for (int x = 0; x < 128; x++)
                                {
                                    var tpoint = terr.ReadUInt16();
                                    var height = (tpoint & 0x0FFF) / 4096.0f;
                                    h[z, x] = height;
                                }
                            }

                            heights.Add(h);
                        }
                    }


                    var defaultHeights = new float[129, 129];
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
                                var h = heights[patchIdx];
                                var rightPatchIdx = x == 79 ? 0xFF : patchConfig[z * 80 + x + 1];
                                var rightHeights = rightPatchIdx == 0xFF ? defaultHeights : heights[rightPatchIdx];
                                for (int xx = 0; xx < 129; xx++)
                                {
                                    h[xx, 128] = rightHeights[xx, 0];
                                }

                                var bottomPatchIdx = z == 79 ? 0xFF : patchConfig[(z + 1) * 80 + x];
                                var bottomHeights = bottomPatchIdx == 0xFF ? defaultHeights : heights[bottomPatchIdx];
                                for (int zz = 0; zz < 129; zz++)
                                {
                                    h[128, zz] = bottomHeights[0, zz];
                                }

                                var bottomRightPatchIdx = z == 79 || x == 79 ? 0xFF : patchConfig[(z + 1) * 80 + x + 1];
                                var bottomRightHeights = bottomRightPatchIdx == 0xFF
                                    ? defaultHeights
                                    : heights[bottomRightPatchIdx];
                                h[128, 128] = bottomRightHeights[0, 0];

                                var tdata = CreateTerrainData();
                                tdata.SetHeights(0, 0, h);
                                mdef.TerrainPatches[x, z] = new TerrainPatch(tdata);

                            }
                        }
                    }
                }

                msn.FindNext("RDEF");
                using (var rdef = new Bwd2Reader(msn))
                {
                    rdef.FindNext("RSEG");
                    while (rdef.Current != null && rdef.Current.Name != "EXIT")
                    {
                        var segmentType = rdef.ReadUInt32();
                        var segmentPieceCount = rdef.ReadUInt32();
                        var road = new Road
                        {
                            SegmentType = (RoadSegmentType)segmentType
                        };
                        
                        for (int i = 0; i < segmentPieceCount; i++)
                        {
                            var roadSegment = new RoadSegment
                            {
                                Left = new Vector3(rdef.ReadSingle(), rdef.ReadSingle(), rdef.ReadSingle()),
                                Right = new Vector3(rdef.ReadSingle(), rdef.ReadSingle(), rdef.ReadSingle())
                            };
                            
                            
                            var localPosition = new Vector3(roadSegment.Left.x % 640, roadSegment.Left.y, roadSegment.Left.z % 640);
                            var patchPosX = (int)(roadSegment.Left.x / 640.0f);
                            var patchPosZ = (int)(roadSegment.Left.z / 640.0f);
                            var terrainPatch = mdef.TerrainPatches[patchPosX, patchPosZ];
                            //TODO: Figure out
                        }
                        rdef.Next();
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
                            v = (byte)(v & 0x7f);
                            if (v != 0)
                                labelBuilder.Append((char)v);
                        }
                        var label = labelBuilder.ToString();
                        var right = new Vector3(odef.ReadSingle(), odef.ReadSingle(), odef.ReadSingle());
                        var upwards = new Vector3(odef.ReadSingle(), odef.ReadSingle(), odef.ReadSingle());
                        var forward = new Vector3(odef.ReadSingle(), odef.ReadSingle(), odef.ReadSingle());
                        var pos = new Vector3(odef.ReadSingle(), odef.ReadSingle(), odef.ReadSingle());
                        odef.BaseStream.Position += 36;
                        var classId = (ClassId)odef.ReadUInt32();
                        odef.ReadUInt16();
                        var teamId = odef.ReadUInt16();

                        var localPosition = new Vector3(pos.x % 640, pos.y, pos.z % 640);
                        var patchPosX = (int)(pos.x / 640.0f);
                        var patchPosZ = (int)(pos.z / 640.0f);
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

                msn.FindNext("LDEF");
                using (var ldef = new Bwd2Reader(msn))
                {
                    ldef.FindNext("OBJ");
                    while (ldef.Current != null && ldef.Current.Name != "EXIT")
                    {
                        var label = new string(ldef.ReadChars(8)).TrimEnd('\0');
                        ldef.BaseStream.Position += 84;
                        var classId = (ClassId)ldef.ReadUInt32();
                        ldef.ReadUInt32();
                        var numStrings = ldef.ReadUInt32();

                        var stringPositions = new List<Vector3>();
                        for (int i = 0; i < numStrings; i++)
                        {
                            var stringPos = new Vector3(ldef.ReadSingle(), ldef.ReadSingle(), ldef.ReadSingle());
                            stringPositions.Add(stringPos);
                        }

                        mdef.StringObjects.Add(new Ldef
                        {
                            Label = label,
                            StringPositions = stringPositions
                        });

                        ldef.Next();
                    }
                }
            }
            return mdef;
        }

        private static TerrainData CreateTerrainData()
        {
            var tdata = new TerrainData();
            tdata.heightmapResolution = 128;
            tdata.size = new Vector3(640, 409.6f, 640);
            return tdata;
        }

        //private static TerrainData ReadTerTerrain(string terPath)
        //{

        //}
    }
}
