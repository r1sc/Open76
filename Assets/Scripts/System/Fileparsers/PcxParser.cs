using System;
using UnityEngine;

namespace Assets.Scripts.System.Fileparsers
{
    public class PcxParser
    {
        private readonly byte[] _buffer;
        
        private PcxParser(byte[] pcxFileData)
        {
            _buffer = pcxFileData;
        }

        private Texture2D Decode(out Color32[] palette)
        {
            int width = BitConverter.ToUInt16(_buffer, 8);
            int height = BitConverter.ToUInt16(_buffer, 10);
            palette = new Color32[256];

            // All PCX enountered need to have an odd number of pixels?
            if ((width & 1) == 0)
                width++;

            // Load the palette
            int i = 0;
            int offset = _buffer.Length - 768;
            while (_buffer[offset] != 0x0c && offset > 0)
            {
                offset--;
            }

            while (i < 256)
            {
                palette[i].r = _buffer[++offset];
                palette[i].g = _buffer[++offset];
                palette[i].b = _buffer[++offset];
                i++;
            }
            
            // Load the image
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);

            offset = 128;
            int x = 0;
            int y = height - 1;
            while (y > 0 && offset < _buffer.Length)
            {
                byte b = _buffer[offset];
                offset++;
                if (b < 0xC0)
                {
                    texture.SetPixel(x, y, palette[b]);

                    x++;
                    if (x > width)
                    {
                        x = 0;
                        y--;
                    }
                }
                else
                {
                    // Repeat byte x-C0h times
                    byte c = _buffer[offset++];
                    while (b > 0xc0)
                    {
                        texture.SetPixel(x, y, palette[c]);

                        x++;
                        if (x > width)
                        {
                            x = 0;
                            y--;
                        }
                        if (y > height)
                            b = 0;
                        else
                            b--;
                    }
                }
            }

            texture.Apply(false, true);
            return texture;
        }
        
        public static Texture2D ParsePcx(byte[] pcxData, out Color32[] palette)
        {
            PcxParser parser = new PcxParser(pcxData);
            return parser.Decode(out palette);
        }
    }
}