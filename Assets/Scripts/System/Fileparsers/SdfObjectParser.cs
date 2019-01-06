using System.Collections.Generic;
using Assets.System;
using UnityEngine;

namespace Assets.Fileparsers
{
    public class Sdf
    {
        public string Name { get; set; }
        public uint Health { get; set; }
        public string DestroySoundName { get; set; }
        public Xdf Xdf { get; set; }
        public SdfPart[] Parts { get; set; }
        public SdfPart WreckedPart { get; set; }
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

        public static Sdf LoadSdf(string filename, bool canWreck)
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
                sdf.Health = br.ReadUInt32();
                string xdfName = br.ReadCString(13) + ".xdf";
                string soundName = br.ReadCString(13);
                if (!string.IsNullOrEmpty(soundName) && soundName.ToLower() != "null")
                {
                    sdf.DestroySoundName = soundName;
                }

                if (VirtualFilesystem.Instance.FileExists(xdfName))
                {
                    sdf.Xdf = XdfParser.ParseXdf(xdfName);
                }

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
                    br.Position += 56;

                    sdf.Parts[i] = sdfPart;
                }

                if (canWreck)
                {
                    SdfPart wreckedPart = GetDestroyedPart(br);
                    if (wreckedPart == null)
                    {
                        wreckedPart = GetDestroyedPart(br);
                    }

                    sdf.WreckedPart = wreckedPart;
                }

                SdfCache.Add(filename, sdf);
                return sdf;
            }
        }
        
        private static SdfPart GetDestroyedPart(Bwd2Reader br)
        {
            string partName = br.ReadCString(8);
            if (string.IsNullOrEmpty(partName) || partName.ToLower() == "null")
            {
                br.Position += 112;
                return null;
            }

            SdfPart wreckedPart = new SdfPart();
            wreckedPart.Name = partName;
            wreckedPart.Right = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            wreckedPart.Up = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            wreckedPart.Forward = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            wreckedPart.Position = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            wreckedPart.ParentName = br.ReadCString(8);
            br.Position += 56;

            return wreckedPart;
        }
    }
}
