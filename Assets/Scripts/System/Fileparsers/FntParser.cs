using System;
using UnityEngine;

namespace Assets.Scripts.System.Fileparsers
{
    public class FntParser
    {
        private readonly byte[] _buffer;

        private FntParser(byte[] fntData)
        {
            _buffer = fntData;
        }

        private struct FntEntry
        {
            public int Offset;
            public int CharWidth;
        }

        private Texture2D Decode()
        {
            if (_buffer[0] != '1' || _buffer[1] != '.' || _buffer[2] != '\0' || _buffer[3] != '\0')
            {
                return null;
            }

            int charCount = BitConverter.ToInt32(_buffer, 4);
            int charHeight = BitConverter.ToInt32(_buffer, 8);
            byte fontBackground = _buffer[12];

            int textureWidth = 0;
            int textureHeight = 32;
            while (textureHeight < charHeight)
            {
                textureHeight *= 2;
            }

            FntEntry[] entries = new FntEntry[charCount];

            for (int i = 0; i < charCount; ++i)
            {
                entries[i].Offset = BitConverter.ToInt32(_buffer, 16 + i * 4);
                entries[i].CharWidth = BitConverter.ToInt32(_buffer, entries[i].Offset);
                textureWidth += entries[i].CharWidth;
            }

            while (textureWidth > 512 && textureHeight < 2048)
            {
                textureWidth /= 2;
                textureHeight *= 2;
            }

            int xPower = 2;
            while (xPower < textureWidth)
            {
                xPower *= 2;
            }

            Color bgColour = new Color32(fontBackground, fontBackground, fontBackground, 255);
            Texture2D texture = new Texture2D(xPower, textureHeight, TextureFormat.RGB24, false);
            int pixelCount = texture.width * texture.height;
            Color32[] pixels = new Color32[pixelCount];
            for (int i = 0; i < pixelCount; ++i)
            {
                pixels[i] = bgColour;
            }
            texture.SetPixels32(pixels);

            int totalWidth = 0;
            int totalHeight = 0;
            for (int i = 0; i < charCount; ++i)
            {
                int offset = entries[i].Offset + 4;
                int charWidth = entries[i].CharWidth;

                int y = 0;
                for (int j = 0; j < charWidth * charHeight; j++)
                {
                    int x = 0;
                    if (x >= charWidth)
                    {
                        ++y;
                        x = 0;
                    }

                    if (offset + j >= _buffer.Length)
                    {
                        break;
                    }

                    byte pixel = _buffer[offset + j];
                    if (pixel != fontBackground)
                    {
                        texture.SetPixel(totalWidth + x, totalHeight + y, Color.white);
                    }

                    ++x;

                    if (totalWidth + x >= textureWidth)
                    {
                        ++totalHeight;
                        totalWidth = 0;
                    }
                }

                totalWidth += charWidth;
            }

            texture.Apply(false, false);
            return texture;
        }

        public static Texture2D ParseFnt(byte[] fntData)
        {
            FntParser parser = new FntParser(fntData);
            return parser.Decode();
        }
    }
}