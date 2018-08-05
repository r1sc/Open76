using Assets.System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Fileparsers
{
    public class ActPaletteParser
    {
        public static Color32[] ReadActPalette(string filename)
        {
            using (var br = VirtualFilesystem.Instance.GetFileStream(filename))
            {
                var colors = new Color32[256];
                for (int i = 0; i < 256; i++)
                {
                    var r = br.ReadByte();
                    var g = br.ReadByte();
                    var b = br.ReadByte();
                    colors[i] = new Color32(r, g, b, 255);
                }
                return colors;
            }
        }
    }
}
