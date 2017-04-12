using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Fileparsers
{
    public class MapTextureParser
    {
        public static Texture2D ReadMapTexture(string filename, Color32[] palette)
        {
            using (var br = new BinaryReader(VirtualFilesystem.Instance.GetFileStream(filename)))
            {
                var width = br.ReadInt32();
                var height = br.ReadInt32();
                var texture = new Texture2D(width, height, TextureFormat.ARGB32, true) {filterMode = FilterMode.Point};
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var paletteIdx = br.ReadByte();
                        texture.SetPixel(x, y, palette[paletteIdx]);
                    }
                }
                texture.Apply(true);
                return texture;
            }
        }
    }
}
