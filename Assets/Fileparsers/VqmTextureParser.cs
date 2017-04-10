using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Fileparsers
{
    class VqmTextureParser
    {
        public static Texture2D ReadVqmTexture(string filename, Color32[] palette)
        {
            using (var br = new BinaryReader(VirtualFilesystem.Instance.GetFileStream(filename)))
            {
                var width = br.ReadInt32();
                var height = br.ReadInt32();
                var texture = new Texture2D(width, height, TextureFormat.ARGB32, true);
                texture.filterMode = FilterMode.Point;
                var cbkFile = new string(br.ReadChars(12)).Replace("\0", "");
                var unk1 = br.ReadUInt32();
                using (var cbkBr = new BinaryReader(VirtualFilesystem.Instance.GetFileStream(cbkFile)))
                {
                    var x = 0;
                    var y = 0;
                    while (br.BaseStream.Position < br.BaseStream.Length)
                    {
                        var index = br.ReadUInt16();
                        if ((index & 0x8000) == 0)
                        {
                            cbkBr.BaseStream.Position = 4 + index*16;
                            for (int sy = 0; sy < 4; sy++)
                            {
                                for (int sx = 0; sx < 4; sx++)
                                {
                                    var paletteIndex = cbkBr.ReadByte();
                                    if (x + sx < width && y - sy > 0)
                                    {
                                        texture.SetPixel(x + sx, y + sy, palette[paletteIndex]);
                                    }
                                }
                            }
                        }
                        else
                        {
                            var paletteIndex = index & 0xFF;
                            for (int sy = 0; sy < 4; sy++)
                            {
                                for (int sx = 0; sx < 4; sx++)
                                {
                                    if (x + sx < width && y - sy > 0)
                                    {
                                        texture.SetPixel(x + sx, y + sy, palette[paletteIndex]);
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
                texture.Apply(true);
                return texture;
            }
        }
    }
}
