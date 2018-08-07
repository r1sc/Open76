using Assets.System;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Fileparsers
{
    class TextureParser
    {
        const FilterMode FilterMode = UnityEngine.FilterMode.Bilinear;
        public static Color32 MaskColor = new Color32(0, 0, 255, 255);
        private static Color32 transparent = new Color32(0, 0, 0, 0);
        private static readonly Dictionary<string, Texture2D> TextureCache = new Dictionary<string, Texture2D>();

        public static Texture2D ReadMapTexture(string filename, Color32[] palette)
        {
            Texture2D texture;
            if (TextureCache.TryGetValue(filename, out texture))
            {
                return texture;
            }

            var hasTransparency = false;
            using (var br = VirtualFilesystem.Instance.GetFileStream(filename))
            {
                var width = br.ReadInt32();
                var height = br.ReadInt32();
                int pixelSize = width * height;
                texture = new Texture2D(width, height, TextureFormat.ARGB32, true)
                {
                    filterMode = FilterMode,
                    wrapMode = TextureWrapMode.Repeat
                };

                int readLimit = (int)Math.Min(br.Length - br.Position, pixelSize);
                if (readLimit > 0)
                {
                    byte[] paletteBytes = br.ReadBytes(readLimit);
                    Color32[] pixelBuffer = new Color32[readLimit];
                    
                    for (int x = 0; x < width; ++x)
                    {
                        for (int y = 0; y < height; ++y)
                        {
                            int colorIndex = x * height + y;
                            if (colorIndex == readLimit)
                            {
                                break;
                            }

                            var paletteIndex = paletteBytes[colorIndex];

                            Color32 color;
                            if (paletteIndex == 0xFF)
                            {
                                hasTransparency = true;
                                color = transparent;
                            }
                            else
                            {
                                if (paletteIndex == 1)
                                {
                                    color = MaskColor; // Set a special color that's easier to filter out.
                                }
                                else
                                {
                                    color = palette[paletteIndex];
                                }
                            }

                            pixelBuffer[colorIndex] = color;
                        }
                    }

                    texture.SetPixels32(pixelBuffer);
                }
                
                if(hasTransparency)
                {
                    texture.wrapMode = TextureWrapMode.Clamp;
                }
                texture.Apply(true);
                TextureCache.Add(filename, texture);
                return texture;
            }
        }

        public static Texture2D ReadVqmTexture(string filename, Color32[] palette)
        {
            Texture2D texture;
            if (TextureCache.TryGetValue(filename, out texture))
            {
                return Texture2D.blackTexture;
            }

            using (var br = VirtualFilesystem.Instance.GetFileStream(filename))
            {
                var width = br.ReadInt32();
                var height = br.ReadInt32();
                var pixels = width * height;
                texture = new Texture2D(width, height, TextureFormat.ARGB32, true)
                {
                    filterMode = FilterMode,
                    wrapMode = TextureWrapMode.Repeat
                };
                var cbkFile = br.ReadCString(12);
                var unk1 = br.ReadInt32();
                bool hasTransparency = false;
                
                if (VirtualFilesystem.Instance.FileExists(cbkFile))
                {
                    Color32[] pixelBuffer = new Color32[pixels];
                    using (var cbkBr = VirtualFilesystem.Instance.GetFileStream(cbkFile))
                    {
                        var x = 0;
                        var y = 0;

                        var byteIndex = 0;
                        var brLength = br.Length - br.Position;
                        var cbkStart = cbkBr.Position;
                        while (byteIndex < brLength)
                        {
                            var index = br.ReadUInt16();
                            byteIndex += sizeof(ushort);

                            if ((index & 0x8000) == 0)
                            {
                                cbkBr.Position = cbkStart + 4 + index * 16;
                                byte[] cbkData = cbkBr.ReadBytes(16);
                                for (int sy = 0; sy < 4; sy++)
                                {
                                    for (int sx = 0; sx < 4; sx++)
                                    {
                                        var paletteIndex = cbkData[sx * 4 + sy];
                                        if (paletteIndex == 0xFF)
                                            hasTransparency = true;
                                        var color = paletteIndex == 0xFF ? transparent : palette[paletteIndex];
                                        int pixelIndex = y * width + sx * width + x + sy;
                                        if (pixelIndex < pixels)
                                        {
                                            pixelBuffer[pixelIndex] = color;
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
                                        int pixelIndex = y * width + sx * width + x + sy;
                                        if (pixelIndex < pixels)
                                        {
                                            pixelBuffer[pixelIndex] = color;
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
                        }
                    }

                    texture.SetPixels32(pixelBuffer);
                }
                else
                {
                    Debug.LogWarning("CBK file not found: " + cbkFile);
                }

                if (hasTransparency)
                {
                    texture.wrapMode = TextureWrapMode.Clamp;
                }
                texture.Apply(true);

                TextureCache.Add(filename, texture);
                return texture;
            }
        }
    }
}
