using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Fileparsers
{
    public class Wdf
    {
        public string Name { get; set; }
        public float Float1 { get; set; }
        public float Float2 { get; set; }
        public float Float3 { get; set; }
        public float Float4 { get; set; }
        public byte Byte1 { get; set; }
        public byte Byte2 { get; set; }
        public byte Byte3 { get; set; }
        public byte Byte4 { get; set; }
        public uint Int1 { get; set; }
        public float Float5 { get; set; }
        public float Radius { get; set; }
        public SdfPart[] Parts { get; set; }
    }

    class WdfParser
    {
        public static Wdf ParseWdf(string filename)
        {
            using (var br = new Bwd2Reader(filename))
            {
                var wdf = new Wdf();

                br.FindNext("WDFC");
                wdf.Name = br.ReadCString(20);
                wdf.Float1 = br.ReadSingle(); //160
                wdf.Float2 = br.ReadSingle(); //100
                wdf.Float3 = br.ReadSingle(); //70
                wdf.Float4 = br.ReadSingle(); //30
                wdf.Byte1 = br.ReadByte(); // 255
                wdf.Byte2 = br.ReadByte(); // 255
                wdf.Byte3 = br.ReadByte(); // 127
                wdf.Byte4 = br.ReadByte(); // 127
                wdf.Int1 = br.ReadUInt32(); // 100

                wdf.Float5 = br.ReadSingle(); //10
                wdf.Radius = br.ReadSingle();

                br.FindNext("WGEO");
                var numParts = br.ReadUInt32();
                wdf.Parts = new SdfPart[numParts];
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

                    wdf.Parts[i] = sdfPart;
                }

                return wdf;
            }
        }
    }
}
