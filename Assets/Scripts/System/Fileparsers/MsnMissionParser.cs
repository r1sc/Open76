using System.Collections.Generic;
using System.Text;
using Assets.Scripts.I76Types;
using UnityEngine;

namespace Assets.Scripts.System.Fileparsers
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
            public bool IsPlayer { get; set; }
            public Quaternion LocalRotation { get; set; }
        }

        public class Ldef
        {
            public string Label { get; set; }
            public Vector3[] StringPositions { get; set; }
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
            public RoadSegment[] RoadSegments { get; set; }
            public RoadSegmentType SegmentType { get; set; }
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
            public List<Road> Roads { get; set; }
            public Vector2 Middle { get; set; }
            public FSM FSM { get; set; }

            public MissonDefinition()
            {
                TerrainPatches = new TerrainPatch[80, 80];
                StringObjects = new List<Ldef>();
                Roads = new List<Road>();
            }
        }

        public static MissonDefinition ReadMsnMission(string filename)
        {
            MissonDefinition mdef = new MissonDefinition();
            using (Bwd2Reader msn = new Bwd2Reader(filename))
            {
                msn.FindNext("WDEF");
                using (Bwd2Reader wdef = new Bwd2Reader(msn))
                {
                    wdef.FindNext("WRLD");
                    wdef.Position += 30;
                    mdef.PaletteFilePath = wdef.ReadCString(13);
                    mdef.LumaTableFilePath = wdef.ReadCString(13);
                    mdef.XlucencyTableFilePath = wdef.ReadCString(13);
                    mdef.ObjectiveFilePath = wdef.ReadCString(13);
                    mdef.SkyTextureFilePath = wdef.ReadCString(13);
                    mdef.ScroungeTextureFilePath = wdef.ReadCString(13);
                    mdef.SurfaceTextureFilePath = wdef.ReadCString(13);
                    mdef.LevelMapFilePath = wdef.ReadCString(13);
                    mdef.HzdFilePath = wdef.ReadCString(13);
                }
                msn.FindNext("TDEF");
                List<float[,]> heights = new List<float[,]>();
                using (Bwd2Reader tdef = new Bwd2Reader(msn))
                {
                    tdef.FindNext("ZMAP");
                    byte numUniqueTerrainPatches = tdef.ReadByte();
                    byte[] patchConfig = tdef.ReadBytes(80 * 80);

                    tdef.FindNext("ZONE");
                    byte unk = tdef.ReadByte();
                    string terrainFilePath = tdef.ReadCString(13);

                    using (FastBinaryReader terr = VirtualFilesystem.Instance.GetFileStream(terrainFilePath))
                    {
                        for (int i = 0; i < numUniqueTerrainPatches; i++)
                        {
                            float[,] h = new float[129, 129];
                            for (int z = 0; z < 128; z++)
                            {
                                for (int x = 0; x < 128; x++)
                                {
                                    ushort tpoint = terr.ReadUInt16();
                                    float height = (tpoint & 0xFFF) / 4096.0f;
                                    h[z, x] = height;
                                }
                            }

                            heights.Add(h);
                        }
                    }

                    Vector2 botLeft = new Vector2(80, 80);
                    Vector2 topRight = new Vector2(0, 0);

                    float[,] defaultHeights = new float[129, 129];
                    for (int z = 0; z < 80; z++)
                    {
                        for (int x = 0; x < 80; x++)
                        {
                            byte patchIdx = patchConfig[z * 80 + x];
                            if (patchIdx == 0xFF)
                            {
                                //mdef.TerrainPatches[x, z] = new TerrainPatch(defaultTerrainData);
                            }
                            else
                            {
                                if (x < botLeft.x)
                                    botLeft.x = x;
                                if (z < botLeft.y)
                                    botLeft.y = z;
                                if (x > topRight.x)
                                    topRight.x = x;
                                if (z > topRight.y)
                                    topRight.y = z;

                                float[,] h = heights[patchIdx];
                                int rightPatchIdx = x == 79 ? 0xFF : patchConfig[z * 80 + x + 1];
                                float[,] rightHeights = rightPatchIdx == 0xFF ? defaultHeights : heights[rightPatchIdx];
                                for (int xx = 0; xx < 129; xx++)
                                {
                                    h[xx, 128] = rightHeights[xx, 0];
                                }

                                int bottomPatchIdx = z == 79 ? 0xFF : patchConfig[(z + 1) * 80 + x];
                                float[,] bottomHeights = bottomPatchIdx == 0xFF ? defaultHeights : heights[bottomPatchIdx];
                                for (int zz = 0; zz < 129; zz++)
                                {
                                    h[128, zz] = bottomHeights[0, zz];
                                }

                                int bottomRightPatchIdx = z == 79 || x == 79 ? 0xFF : patchConfig[(z + 1) * 80 + x + 1];
                                float[,] bottomRightHeights = bottomRightPatchIdx == 0xFF
                                    ? defaultHeights
                                    : heights[bottomRightPatchIdx];
                                h[128, 128] = bottomRightHeights[0, 0];

                                TerrainData tdata = CreateTerrainData();
                                tdata.SetHeights(0, 0, h);
                                mdef.TerrainPatches[x, z] = new TerrainPatch(tdata);

                            }
                        }
                    }
                    mdef.Middle = (topRight + botLeft) / 2.0f;

                }

                msn.FindNext("RDEF");
                using (Bwd2Reader rdef = new Bwd2Reader(msn))
                {
                    rdef.FindNext("RSEG");
                    while (rdef.Current != null && rdef.Current.Name != "EXIT")
                    {
                        uint segmentType = rdef.ReadUInt32();
                        uint segmentPieceCount = rdef.ReadUInt32();
                        Road road = new Road
                        {
                            SegmentType = (RoadSegmentType)segmentType,
                            RoadSegments = new RoadSegment[segmentPieceCount]
                        };

                        for (int i = 0; i < segmentPieceCount; i++)
                        {
                            RoadSegment roadSegment = new RoadSegment
                            {
                                Left = new Vector3(rdef.ReadSingle(), rdef.ReadSingle(), rdef.ReadSingle()),
                                Right = new Vector3(rdef.ReadSingle(), rdef.ReadSingle(), rdef.ReadSingle())
                            };

                            Vector3 pos = roadSegment.Left;
                            int patchPosX = (int)(pos.x / 640.0f);
                            int patchPosZ = (int)(pos.z / 640.0f);
                            float localPositionX = (pos.x % 640) / 640.0f;
                            float localPositionZ = (pos.z % 640) / 640.0f;
                            float y =
                                mdef.TerrainPatches[patchPosX, patchPosZ].TerrainData.GetInterpolatedHeight(localPositionX,
                                    localPositionZ) + 0.1f;
                            pos.y = y;
                            roadSegment.Left = pos;

                            pos = roadSegment.Right;
                            patchPosX = (int)(pos.x / 640.0f);
                            patchPosZ = (int)(pos.z / 640.0f);
                            localPositionX = (pos.x % 640) / 640.0f;
                            localPositionZ = (pos.z % 640) / 640.0f;
                            y =
                                mdef.TerrainPatches[patchPosX, patchPosZ].TerrainData.GetInterpolatedHeight(localPositionX,
                                    localPositionZ) + 0.1f;
                            pos.y = y;
                            roadSegment.Right = pos;

                            road.RoadSegments[i] = roadSegment;
                            //TODO: Figure out
                        }
                        mdef.Roads.Add(road);

                        rdef.Next();
                    }
                }

                msn.FindNext("ODEF");
                using (Bwd2Reader odef = new Bwd2Reader(msn))
                {
                    odef.FindNext("OBJ");
                    while (odef.Current.Name != "EXIT")
                    {
                        byte[] rawlabel = odef.ReadBytes(8);
                        int labelhigh = 0;
                        StringBuilder labelBuilder = new StringBuilder();
                        for (int i = 0; i < 8; i++)
                        {
                            byte v = rawlabel[i];
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
                        string label = labelBuilder.ToString();
                        Vector3 right = new Vector3(odef.ReadSingle(), odef.ReadSingle(), odef.ReadSingle());
                        Vector3 upwards = new Vector3(odef.ReadSingle(), odef.ReadSingle(), odef.ReadSingle());
                        Vector3 forward = new Vector3(odef.ReadSingle(), odef.ReadSingle(), odef.ReadSingle());
                        Vector3 pos = new Vector3(odef.ReadSingle(), odef.ReadSingle(), odef.ReadSingle());

                        odef.Position += 36;
                        ClassId classId = (ClassId)odef.ReadUInt32();
                        ushort flags = odef.ReadUInt16();
                        ushort teamId = odef.ReadUInt16();

                        Vector3 localPosition = new Vector3(pos.x % 640, pos.y, pos.z % 640);
                        int patchPosX = (int)(pos.x / 640.0f);
                        int patchPosZ = (int)(pos.z / 640.0f);
                        mdef.TerrainPatches[patchPosX, patchPosZ].Objects.Add(new Odef
                        {
                            Label = label,
                            Id = labelhigh,
                            LocalPosition = localPosition,
                            ClassId = classId,
                            TeamId = teamId,
                            IsPlayer = flags == 16,
                            LocalRotation = Quaternion.LookRotation(forward, upwards)
                        });

                        odef.Next();
                    }
                }

                msn.FindNext("LDEF");
                using (Bwd2Reader ldef = new Bwd2Reader(msn))
                {
                    ldef.FindNext("OBJ");
                    while (ldef.Current != null && ldef.Current.Name != "EXIT")
                    {
                        string label = ldef.ReadCString(8);
                        ldef.Position += 84;
                        ClassId classId = (ClassId)ldef.ReadUInt32();
                        ldef.ReadUInt32();
                        uint numStrings = ldef.ReadUInt32();

                        Vector3[] stringPositions = new Vector3[numStrings];
                        for (int i = 0; i < numStrings; i++)
                        {
                            Vector3 stringPos = new Vector3(ldef.ReadSingle(), ldef.ReadSingle(), ldef.ReadSingle());
                            stringPositions[i] = stringPos;
                        }

                        mdef.StringObjects.Add(new Ldef
                        {
                            Label = label,
                            StringPositions = stringPositions
                        });

                        ldef.Next();
                    }
                }

                msn.FindNext("ADEF");
                using (Bwd2Reader adef = new Bwd2Reader(msn))
                {
                    adef.FindNext("FSM");
                    if (adef.Current != null && adef.Current.DataLength > 0)
                    {
                        mdef.FSM = new FSM();

                        mdef.FSM.ActionTable = new string[adef.ReadUInt32()];
                        for (int i = 0; i < mdef.FSM.ActionTable.Length; i++)
                        {
                            mdef.FSM.ActionTable[i] = adef.ReadCString(40);
                        }

                        uint numEntities = adef.ReadUInt32();
                        mdef.FSM.EntityTable = new FSMEntity[numEntities];

                        for (int i = 0; i < numEntities; i++)
                        {
                            string label = adef.ReadCString(40);
                            byte[] rawlabel = adef.ReadBytes(8);

                            int labelhigh = 0;
                            StringBuilder labelBuilder = new StringBuilder();
                            for (int j = 0; j < 8; j++)
                            {
                                byte v = rawlabel[j];
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
                                    labelBuilder.Append((char) v);
                            }

                            mdef.FSM.EntityTable[i] = new FSMEntity
                            {
                                Label = label,
                                Value = labelBuilder.ToString(),
                                Id = labelhigh
                            };
                        }

                        mdef.FSM.SoundClipTable = new string[adef.ReadUInt32()];
                        for (int i = 0; i < mdef.FSM.SoundClipTable.Length; i++)
                        {
                            mdef.FSM.SoundClipTable[i] = adef.ReadCString(40);
                        }

                        uint numPaths = adef.ReadUInt32();
                        mdef.FSM.Paths = new FSMPath[numPaths];
                        for (int i = 0; i < numPaths; i++)
                        {
                            string name = adef.ReadCString(40);
                            I76Vector3[] nodes = new I76Vector3[adef.ReadUInt32()];
                            for (int p = 0; p < nodes.Length; p++)
                            {
                                nodes[p] = new I76Vector3(adef.ReadSingle(), adef.ReadSingle(), adef.ReadSingle());
                            }

                            mdef.FSM.Paths[i] = new FSMPath
                            {
                                Name = name,
                                Nodes = nodes
                            };
                        }

                        uint numMachines = adef.ReadUInt32();
                        mdef.FSM.StackMachines = new StackMachine[numMachines];
                        for (int i = 0; i < numMachines; i++)
                        {
                            long next = adef.Position + 168;

                            StackMachine machine = new StackMachine();
                            machine.StartAddress = adef.ReadUInt32();
                            machine.InitialArguments = new int[adef.ReadUInt32()];

                            for (int j = 0; j < machine.InitialArguments.Length; j++)
                            {
                                machine.InitialArguments[j] = adef.ReadInt32();
                            }

                            adef.Position = next;

                            mdef.FSM.StackMachines[i] = machine;
                        }

                        mdef.FSM.Constants = new IntRef[adef.ReadUInt32()];
                        for (int i = 0; i < mdef.FSM.Constants.Length; i++)
                        {
                            mdef.FSM.Constants[i] = new IntRef(adef.ReadInt32());
                        }

                        mdef.FSM.ByteCode = new ByteCode[adef.ReadUInt32()];
                        for (int i = 0; i < mdef.FSM.ByteCode.Length; i++)
                        {
                            ByteCode byteCode = mdef.FSM.ByteCode[i] = new ByteCode();
                            byteCode.OpCode = (OpCode) adef.ReadUInt32();
                            byteCode.Value = adef.ReadInt32();
                        }
                    }
                }
            }
            return mdef;
        }

        private static TerrainData CreateTerrainData()
        {
            TerrainData tdata = new TerrainData();
            tdata.heightmapResolution = 128;
            tdata.size = new Vector3(640, 409.5f, 640);
            return tdata;
        }

        //private static TerrainData ReadTerTerrain(string terPath)
        //{

        //}
    }
}
