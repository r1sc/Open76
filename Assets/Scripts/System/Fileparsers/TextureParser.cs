﻿using Assets.System;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Fileparsers
{
    class TextureParser
    {
        private static readonly Color32 Transparent = new Color32(0, 0, 0, 0);
        private static readonly Dictionary<string, Texture2D> TextureCache = new Dictionary<string, Texture2D>();
        public static readonly Dictionary<string, Texture2D> MaskTextureCache = new Dictionary<string, Texture2D>();

        public static Texture2D ReadMapTexture(string filename, Color32[] palette, TextureFormat format = TextureFormat.RGBA32, bool makeReadOnly = false, FilterMode filterMode = FilterMode.Bilinear)
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
                texture = new Texture2D(width, height, format, true)
                {
                    filterMode = filterMode,
                    wrapMode = TextureWrapMode.Repeat
                };

                int readLimit = (int)Math.Min(br.Length - br.Position, pixelSize);
                if (readLimit > 0)
                {
                    byte[] paletteBytes = br.ReadBytes(readLimit);
                    Color32[] pixelBuffer = new Color32[readLimit];
                    Color32[] maskBuffer = null;
                    bool maskCached = false;
                    
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
                                color = Transparent;
                            }
                            else
                            {
                                if (paletteIndex == 1 && !maskCached)
                                {
                                    if (maskBuffer == null)
                                    {
                                        if (MaskTextureCache.ContainsKey(filename))
                                        {
                                            maskCached = true;
                                        }
                                        else
                                        {
                                            maskBuffer = new Color32[readLimit];
                                            maskBuffer[colorIndex].a = 255;
                                        }
                                    }
                                    else
                                    {
                                        maskBuffer[colorIndex].a = 255;
                                    }

                                    color = Color.black;
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

                    if (maskBuffer != null)
                    {
                        Texture2D maskTexture = new Texture2D(width, height, TextureFormat.Alpha8, false);
                        maskTexture.SetPixels32(maskBuffer);
                        maskTexture.Apply(false, true);
                        MaskTextureCache.Add(filename.ToUpper(), maskTexture);
                    }
                }
                
                if(hasTransparency)
                {
                    texture.wrapMode = TextureWrapMode.Clamp;
                }
                texture.Apply(true, makeReadOnly);
                TextureCache.Add(filename, texture);
                return texture;
            }
        }

        public static Texture2D ReadVqmTexture(string filename, Color32[] palette)
        {
            Texture2D texture;
            if (TextureCache.TryGetValue(filename, out texture))
            {
                return texture;
            }

            using (var br = VirtualFilesystem.Instance.GetFileStream(filename))
            {
                var width = br.ReadInt32();
                var height = br.ReadInt32();
                var pixels = width * height;
                texture = new Texture2D(width, height, TextureFormat.ARGB32, true)
                {
                    filterMode = FilterMode.Bilinear,
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
                                        var color = paletteIndex == 0xFF ? Transparent : palette[paletteIndex];
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
                                var color = paletteIndex == 0xFF ? Transparent : palette[paletteIndex];
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
