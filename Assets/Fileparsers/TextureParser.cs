using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Fileparsers
{
    class TextureParser
    {
        private static readonly Dictionary<string, Texture2D> TextureCache = new Dictionary<string, Texture2D>();

        public static Texture2D ReadMapTexture(string filename, Color32[] palette)
        {
            filename = filename.ToLower();
            if (TextureCache.ContainsKey(filename))
                return TextureCache[filename];

            using (var br = new BinaryReader(VirtualFilesystem.Instance.GetFileStream(filename)))
            {
                var width = br.ReadInt32();
                var height = br.ReadInt32();
                var texture = new Texture2D(width, height, TextureFormat.ARGB32, true) { filterMode = FilterMode.Point };
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var paletteIdx = br.ReadByte();
                        texture.SetPixel(x, y, palette[paletteIdx]);
                    }
                }
                texture.Apply(true);
                TextureCache.Add(filename, texture);
                return texture;
            }
        }

        public static Texture2D ReadVqmTexture(string filename, Color32[] palette, string lumaFile)
        {
            filename = filename.ToLower();
            if (TextureCache.ContainsKey(filename))
                return TextureCache[filename];

            using (var br = new BinaryReader(VirtualFilesystem.Instance.GetFileStream(filename)))
            {
                var width = br.ReadInt32();
                var height = br.ReadInt32();
                var texture = new Texture2D(width, height, TextureFormat.ARGB32, true);
                texture.filterMode = FilterMode.Point;
                var cbkFile = new string(br.ReadChars(12)).Replace("\0", "");
                var unk1 = br.ReadInt32();
                var transparent = new Color32(0, 0, 0, 0);
                bool hasTransparency = false;

                using (var cbkBr = new BinaryReader(VirtualFilesystem.Instance.GetFileStream(cbkFile)))
                {
                    var x = 0;
                    var y = 0;
                    while (br.BaseStream.Position < br.BaseStream.Length)
                    {
                        var index = br.ReadUInt16();
                        if ((index & 0x8000) == 0)
                        {
                            cbkBr.BaseStream.Position = 4 + index * 16;
                            for (int sy = 0; sy < 4; sy++)
                            {
                                for (int sx = 0; sx < 4; sx++)
                                {
                                    var paletteIndex = cbkBr.ReadByte();
                                    if (paletteIndex == 0xFF)
                                        hasTransparency = true;
                                    var color = paletteIndex == 0xFF ? transparent : palette[paletteIndex];
                                    if (x + sx < width && y + sy < height)
                                    {
                                        texture.SetPixel(x + sx, y + sy, color);
                                    }
                                }
                            }
                        }
                        else
                        {
                            var paletteIndex = index & 0xFF;
                            if (paletteIndex == 0xFF)
                                hasTransparency = true;
                            var color = paletteIndex == 0xFF ? transparent : palette[paletteIndex];
                            for (int sy = 0; sy < 4; sy++)
                            {
                                for (int sx = 0; sx < 4; sx++)
                                {
                                    if (x + sx < width && y + sy < height)
                                    {
                                        texture.SetPixel(x + sx, y + sy, color);
                                    }
                                }
                            }
                        }
                        x += 4;
                        if (x >= width)
                        {
                            x = 0;
                            y += 4;
                        }
                        if (y >= height)
                        {
                            break;
                        }
                    }
                }
                texture.alphaIsTransparency = hasTransparency;
                texture.Apply(true);

                TextureCache.Add(filename, texture);
                return texture;
            }
        }
    }
}
