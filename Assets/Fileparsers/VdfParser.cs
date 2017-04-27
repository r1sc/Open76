using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Fileparsers
{
    public class WheelLoc
    {
        public Vector3 Right { get; set; }
        public Vector3 Up { get; set; }
        public Vector3 Forward { get; set; }
        public Vector3 Position { get; set; }
    }

    public class Vdf
    {
        public float Unk70f;
        public float Unk35f;
        public float Unk30f;
        public float Unk25f;
        public byte Unk255b;
        public byte Unk255b2;
        public byte Unk127b;
        public byte Unk127b2;
        public float Unk1320f;
        public float Unk1f;
        public byte Unk63b;
        public byte Unk82b;
        public byte Unk73b;
        public byte Unk157b;
        public byte Unk57b;
        public string Name { get; set; }
        public uint Unk0 { get; set; }
        public uint Unk1 { get; set; }
        public uint Unk2 { get; set; }
        public uint Unk4 { get; set; }
        public List<SdfPart[]> PartsThirdPerson { get; set; }
        public SdfPart[] PartsFirstPerson { get; set; }
        public WheelLoc[] WheelLoc { get; set; }
    }

    public class VdfParser
    {
        public static Vdf ParseVdf(string filename)
        {
            using (var br = new Bwd2Reader(filename))
            {
                br.FindNext("VDFC");

                var vdf = new Vdf();
                vdf.Name = br.ReadCString(16);
                vdf.Unk0 = br.ReadUInt32();
                vdf.Unk1 = br.ReadUInt32();
                vdf.Unk2 = br.ReadUInt32();
                vdf.Unk70f = br.ReadSingle();
                vdf.Unk35f = br.ReadSingle();
                vdf.Unk30f = br.ReadSingle();
                vdf.Unk25f = br.ReadSingle();
                vdf.Unk255b = br.ReadByte();
                vdf.Unk255b2 = br.ReadByte();
                vdf.Unk127b = br.ReadByte();
                vdf.Unk127b2 = br.ReadByte();
                vdf.Unk1320f = br.ReadSingle();
                vdf.Unk1f = br.ReadSingle();
                vdf.Unk63b = br.ReadByte();
                vdf.Unk82b = br.ReadByte();
                vdf.Unk73b = br.ReadByte();
                vdf.Unk157b = br.ReadByte();
                vdf.Unk57b = br.ReadByte();


                vdf.Unk4 = br.ReadUInt32();

                br.FindNext("VGEO");
                var numParts = br.ReadUInt32();
                vdf.PartsThirdPerson = new List<SdfPart[]>(4);
                for (int damageState = 0; damageState < 4; damageState++)
                {
                    var parts = new SdfPart[numParts];
                    for (int i = 0; i < numParts; i++)
                    {
                        var sdfPart = new SdfPart();
                        sdfPart.Name = br.ReadCString(8);
                        sdfPart.Right = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                        sdfPart.Up = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                        sdfPart.Forward = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                        sdfPart.Position = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                        sdfPart.ParentName = br.ReadCString(8);
                        br.BaseStream.Seek(36, SeekOrigin.Current);

                        parts[i] = sdfPart;
                    }
                    vdf.PartsThirdPerson.Add(parts);
                }
                br.BaseStream.Seek(100 * numParts * 12, SeekOrigin.Current);

                vdf.PartsFirstPerson = new SdfPart[numParts];
                for (int i = 0; i < numParts; i++)
                {
                    var sdfPart = new SdfPart();
                    sdfPart.Name = br.ReadCString(8);
                    sdfPart.Right = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    sdfPart.Up = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    sdfPart.Forward = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    sdfPart.Position = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    sdfPart.ParentName = br.ReadCString(8);
                    br.BaseStream.Seek(36, SeekOrigin.Current);

                    vdf.PartsFirstPerson[i] = sdfPart;
                }

                br.FindNext("WLOC");
                vdf.WheelLoc = new WheelLoc[6];
                for (int i = 0; i < 6; i++)
                {
                    var wheelLoc = vdf.WheelLoc[i] = new WheelLoc();
                    var unk1 = br.ReadUInt32();
                    wheelLoc.Right = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    wheelLoc.Up = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    wheelLoc.Forward = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    wheelLoc.Position = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    var unk2 = br.ReadSingle();
                }

                return vdf;
            }
        }
    }
}
