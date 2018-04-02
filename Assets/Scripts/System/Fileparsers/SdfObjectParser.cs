using Assets.System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Fileparsers
{
    public class Sdf
    {
        public string Name { get; set; }
        public SdfPart[] Parts { get; set; }

    }

    public class SdfPart
    {
        public string Name { get; set; }
        public string ParentName { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Right { get; set; }
        public Vector3 Up { get; set; }
        public Vector3 Forward { get; set; }
    }

    public class SdfObjectParser
    {
        private static readonly Dictionary<string, Sdf> SdfCache = new Dictionary<string, Sdf>();

        public static Sdf LoadSdf(string filename)
        {
            filename = filename.ToLower();
            if (SdfCache.ContainsKey(filename))
                return SdfCache[filename];

            using (var br = new Bwd2Reader(filename))
            {
                var sdf = new Sdf();

                br.FindNext("SDFC");
                sdf.Name = br.ReadCString(16);
                var one = br.ReadUInt32();
                var size = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                var unk1 = br.ReadUInt32();
                var unk2 = br.ReadUInt32();
                var fifty = br.ReadUInt32();
                var xdf = br.ReadCString(13);
                var wav = br.ReadCString(13);
                
                br.FindNext("SGEO");
                var numParts = br.ReadUInt32();
                sdf.Parts = new SdfPart[numParts];
                for (int i = 0; i < numParts; i++)
                {
                    var sdfPart = new SdfPart();
                    sdfPart.Name = br.ReadCString(8);
                    sdfPart.Right = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    sdfPart.Up = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    sdfPart.Forward = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    sdfPart.Position = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    sdfPart.ParentName = br.ReadCString(8);
                    br.BaseStream.Seek(56, SeekOrigin.Current);

                    sdf.Parts[i] = sdfPart;
                }

                SdfCache.Add(filename, sdf);
                return sdf;
            }
        }
    }
}
