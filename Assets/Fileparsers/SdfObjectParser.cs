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
    }

    public class SdfObjectParser
    {
        public static Sdf LoadSdf(string filename)
        {
            using (var br = new Bwd2Reader(filename))
            {
                var sdf = new Sdf();

                br.FindNext("SDFC");
                sdf.Name = new string(br.ReadChars(16)).TrimEnd('\0');
                var one = br.ReadUInt32();
                Debug.Log(".sdf One: " + one);
                var size = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                var unk1 = br.ReadUInt32();
                var unk2 = br.ReadUInt32();
                var fifty = br.ReadUInt32();
                Debug.Log(".sdf Fifty: " + fifty);
                var xdf = new string(br.ReadChars(13)).TrimEnd('\0');
                var wav = new string(br.ReadChars(13)).TrimEnd('\0');
                
                br.FindNext("SGEO");
                var numParts = br.ReadUInt32();
                sdf.Parts = new SdfPart[numParts];
                for (int i = 0; i < numParts; i++)
                {
                    var sdfPart = new SdfPart();
                    sdfPart.Name = new string(br.ReadChars(8)).TrimEnd('\0');
                    var xScale = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    var yScale = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    var zScale = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    if(xScale != Vector3.right)
                        Debug.Log(".sdf X discrepancy:" + xScale);
                    if (yScale != Vector3.up)
                        Debug.Log(".sdf Y discrepancy:" + yScale);
                    if (zScale != Vector3.forward)
                        Debug.Log(".sdf Z discrepancy:" + zScale);
                    sdfPart.Position = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    sdfPart.ParentName = new string(br.ReadChars(8)).TrimEnd('\0');
                    br.BaseStream.Seek(56, SeekOrigin.Current);

                    sdf.Parts[i] = sdfPart;
                }

                return sdf;
            }
        }
    }
}
