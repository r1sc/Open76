using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.System.Fileparsers
{
    public class WheelLoc
    {
        public Vector3 Right { get; set; }
        public Vector3 Up { get; set; }
        public Vector3 Forward { get; set; }
        public Vector3 Position { get; set; }
    }

    public class VLoc
    {
        public uint Number { get; set; }
        public Vector3 Right { get; set; }
        public Vector3 Up { get; set; }
        public Vector3 Forward { get; set; }
        public Vector3 Position { get; set; }
    }

    public class HLoc
    {
        public string Label { get; set; }
        public Vector3 Right { get; set; }
        public Vector3 Up { get; set; }
        public Vector3 Forward { get; set; }
        public Vector3 Position { get; set; }
        public float Unk { get; set; }
        public uint HardpointIndex { get; set; }
        public uint FacingDirection { get; set; }
        public HardpointMeshType MeshType { get; set; }
    }

    public class ETbl
    {
        public string MapFile { get; set; }
        public bool IsReferenceImage { get; set; }
        public uint ItemCount { get; set; }
        public Dictionary<string, ETblItem> Items { get; set; }

        public class ETblItem
        {
            public string Name { get; set; }
            public int XOffset { get; set; }
            public int YOffset { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
        }
    }

    public enum HardpointMeshType : byte
    {
        None,
        Top,
        Side,
        Turret,
        Dropper,
        Inside
    }

    public class Vdf
    {
        public float LODDistance2;
        public float LODDistance3;
        public float LODDistance4;
        public float LODDistance5;
        public float Mass;
        public float CollisionMultiplier;
        public float DragCoefficient;
        public uint Unknown;
        public string Name { get; set; }
        public string EltFile { get; set; }
        public uint VehicleType { get; set; }
        public uint VehicleSize { get; set; }
        public float LODDistance1 { get; set; }
        public uint Unk4 { get; set; }
        public List<SdfPart[]> PartsThirdPerson { get; set; }
        public SdfPart[] PartsFirstPerson { get; set; }
        public Bounds BoundsInner { get; internal set; }
        public Bounds BoundsOuter { get; internal set; }
        public WheelLoc[] WheelLoc { get; set; }
        public List<VLoc> VLocs { get; set; }
        public string SOBJGeoName { get; set; }
        public List<HLoc> HLocs { get; set; }
        public List<ETbl> Etbls { get; set; }
    }

    public class VdfParser
    {
        public static Vdf ParseVdf(string filename)
        {
            using (Bwd2Reader br = new Bwd2Reader(filename))
            {
                Vdf vdf = new Vdf();
                br.FindNext("VDFC");
                
                vdf.Name = br.ReadCString(20);
                vdf.VehicleType = br.ReadUInt32();
                vdf.VehicleSize = br.ReadUInt32();
                vdf.LODDistance1 = br.ReadSingle();
                vdf.LODDistance2 = br.ReadSingle();
                vdf.LODDistance3 = br.ReadSingle();
                vdf.LODDistance4 = br.ReadSingle();
                vdf.LODDistance5 = br.ReadSingle();
                vdf.Mass = br.ReadSingle();
                vdf.CollisionMultiplier = br.ReadSingle();
                vdf.DragCoefficient = br.ReadSingle();
                vdf.Unknown = br.ReadUInt32();
                if (br.Current.DataLength == 77)
                {
                    vdf.EltFile = br.ReadCString(13);
                }

                br.FindNext("SOBJ");
                vdf.SOBJGeoName = br.ReadCString(8);

                vdf.VLocs = new List<VLoc>();
                br.FindNext("VLOC");
                while (br.Current != null && br.Current.Name != "EXIT")
                {
                    VLoc vloc = new VLoc
                    {
                        Number = br.ReadUInt32(),
                        Right = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                        Up = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                        Forward = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                        Position = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle())
                    };
                    vdf.VLocs.Add(vloc);

                    br.Next();
                }
                
                br.FindNext("VGEO");
                uint numParts = br.ReadUInt32();
                vdf.PartsThirdPerson = new List<SdfPart[]>(4);
                for (int damageState = 0; damageState < 4; damageState++)
                {
                    SdfPart[] parts = new SdfPart[numParts];
                    for (int i = 0; i < numParts; i++)
                    {
                        SdfPart sdfPart = new SdfPart();
                        sdfPart.Name = br.ReadCString(8);
                        sdfPart.Right = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                        sdfPart.Up = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                        sdfPart.Forward = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                        sdfPart.Position = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                        sdfPart.ParentName = br.ReadCString(8);
                        br.Position += 36;

                        parts[i] = sdfPart;
                    }
                    vdf.PartsThirdPerson.Add(parts);
                }
                br.Position += 100 * numParts * 12;

                vdf.PartsFirstPerson = new SdfPart[numParts];
                for (int i = 0; i < numParts; i++)
                {
                    SdfPart sdfPart = new SdfPart();
                    sdfPart.Name = br.ReadCString(8);
                    sdfPart.Right = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    sdfPart.Up = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    sdfPart.Forward = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    sdfPart.Position = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    sdfPart.ParentName = br.ReadCString(8);
                    br.Position += 36;

                    vdf.PartsFirstPerson[i] = sdfPart;
                }

                br.FindNext("COLP");

                float zMaxOuter = br.ReadSingle();
                float zMaxInner = br.ReadSingle();
                float zMinInner = br.ReadSingle();
                float zMinOuter = br.ReadSingle();

                float xMaxOuter = br.ReadSingle();
                float xMaxInner = br.ReadSingle();
                float xMinInner = br.ReadSingle();
                float xMinOuter = br.ReadSingle();

                float yMaxOuter = br.ReadSingle();
                float yMaxInner = br.ReadSingle();
                float yMinInner = br.ReadSingle();
                float yMinOuter = br.ReadSingle();

                Bounds innerBounds = new Bounds();
                innerBounds.SetMinMax(new Vector3(xMinInner, yMinInner, zMinInner), new Vector3(xMaxInner, yMaxInner, zMaxInner));
                vdf.BoundsInner = innerBounds;

                Bounds outerBounds = new Bounds();
                outerBounds.SetMinMax(new Vector3(xMinOuter, yMinOuter, zMinOuter), new Vector3(xMaxOuter, yMaxOuter, zMaxOuter));
                vdf.BoundsOuter = outerBounds;

                br.FindNext("WLOC");
                vdf.WheelLoc = new WheelLoc[6];
                for (int i = 0; i < 6; i++)
                {
                    WheelLoc wheelLoc = vdf.WheelLoc[i] = new WheelLoc();
                    uint unk1 = br.ReadUInt32();
                    wheelLoc.Right = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    wheelLoc.Up = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    wheelLoc.Forward = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    wheelLoc.Position = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    float unk2 = br.ReadSingle();
                }

                vdf.HLocs = new List<HLoc>();
                br.FindNext("HLOC");
                while (br.Current != null && br.Current.Name != "EXIT")
                {
                    HLoc hloc = new HLoc
                    {
                        Label = br.ReadCString(16),
                        HardpointIndex = br.ReadUInt32(),
                        FacingDirection = br.ReadUInt32(),
                        MeshType = (HardpointMeshType)br.ReadUInt32(),
                        Right = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                        Up = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                        Forward = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                        Position = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                        Unk = br.ReadSingle()
                    };
                    vdf.HLocs.Add(hloc);

                    br.Next();
                }

                if (!SpriteManager.Instance.Initialised)
                {
                    if (vdf.EltFile == null)
                    {
                        vdf.Etbls = new List<ETbl>();
                        br.FindNext("ETBL");

                        while (br.Current != null && br.Current.Name != "EXIT")
                        {
                            long tableEnd = br.Current.DataPosition + br.Current.DataLength;
                            while (br.Position < tableEnd)
                            {
                                ETbl etbl = new ETbl
                                {
                                    MapFile = br.ReadCString(13),
                                    IsReferenceImage = br.ReadUInt32() == 1,
                                    ItemCount = br.ReadUInt32(),
                                };

                                uint itemCount = etbl.ItemCount;
                                etbl.Items = new Dictionary<string, ETbl.ETblItem>((int) itemCount);
                                for (uint i = 0; i < itemCount; ++i)
                                {
                                    ETbl.ETblItem etblItem = new ETbl.ETblItem
                                    {
                                        Name = br.ReadCString(16),
                                        XOffset = br.ReadInt32(),
                                        YOffset = br.ReadInt32(),
                                        Width = br.ReadInt32(),
                                        Height = br.ReadInt32()
                                    };

                                    etbl.Items.Add(etblItem.Name, etblItem);
                                }

                                vdf.Etbls.Add(etbl);
                            }

                            br.Next();
                        }
                    }
                    else
                    {
                        vdf.Etbls = EltParser.ParseElt(vdf.EltFile);
                    }

                    if (vdf.Etbls != null && vdf.Etbls.Count > 0)
                    {
                        SpriteManager.Instance.Initialise(vdf);
                    }
                }

                return vdf;
            }
        }
    }
}
