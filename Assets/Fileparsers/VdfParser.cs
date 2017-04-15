using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Fileparsers
{
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
    }

    public class VdfParser
    {
        public static void ParseVdf(string filename)
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
            }
        }
    }
}
