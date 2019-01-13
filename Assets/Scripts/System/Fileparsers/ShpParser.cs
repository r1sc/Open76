using System;
using UnityEngine;

namespace Assets.Scripts.System.Fileparsers
{
    public class ShpParser
    {
        private readonly byte[] _buffer;
        private readonly Color32[] _palette;

        private ShpParser(byte[] shpFileData, Color32[] palette)
        {
            _buffer = shpFileData;
            _palette = palette;
        }

        private struct Shp
        {
            public int Offset;
            public short Height;    // the x dimension of the image
            public short Width;    // the y dimension of the image
            public short YOrigin;   // the "hot spot" of the image
            public short XOrigin;   // 
            public int XMin;       // the left-most   pixel coordinate in the shape
            public int YMin;       // the top-most    pixel coordinate in the shape
            public int XMax;       // the right-most  pixel coordinate in the shape
            public int YMax;       // the bottom-most pixel coordinate in the shape
        }

        private Shp ParseShape(int shapeIndex)
        {
            Shp shape;
            shape.Offset = BitConverter.ToInt32(_buffer, 8 + 8 * shapeIndex);
            shape.Height = BitConverter.ToInt16(_buffer, shape.Offset);
            shape.Width = BitConverter.ToInt16(_buffer, shape.Offset + 2);
            shape.XOrigin = BitConverter.ToInt16(_buffer, shape.Offset + 4);
            shape.YOrigin = BitConverter.ToInt16(_buffer, shape.Offset + 6);
            shape.XMin = BitConverter.ToInt32(_buffer, shape.Offset + 8);
            shape.YMin = BitConverter.ToInt32(_buffer, shape.Offset + 12);
            shape.XMax = BitConverter.ToInt32(_buffer, shape.Offset + 16);
            shape.YMax = BitConverter.ToInt32(_buffer, shape.Offset + 20);
            
            return shape;
        }
        
        private Texture2D[] Decode()
        {
            // Read and check basic numbers
            if (_buffer.Length < 8)
            {
                return null;
            }

            // Check header
            if (_buffer[0] != '1' || _buffer[1] != '.' || _buffer[2] != '1' || _buffer[3] != '0')
            {
                return null;
            }

            int shapeCount = BitConverter.ToInt32(_buffer, 4);
            if (shapeCount == 0)
            {
                return null;
            }

            Shp[] shapes = new Shp[shapeCount];
            for (int i = 0; i < shapeCount; ++i)
            {
                shapes[i] = ParseShape(i);
            }
            
            Texture2D[] textures = new Texture2D[shapeCount];
            for (int i = 0; i < shapeCount; ++i)
            {
                textures[i] = new Texture2D(shapes[i].Width + 1, shapes[i].Height + 1, TextureFormat.RGB24, false);
            }
            
            // SHP decoder written by Eric C. Peterson, licensed under UoI/NCSA.
            // Revision history:
            //      + 2009 May 13 : Clean up, public release on mw2.jjaro.net
            //      + 2006 Oct 27 : Initial version written
            for (int i = 0; i < shapeCount; ++i)
            {
                int line = shapes[i].Height + 1;
                int offset = shapes[i].Offset + 24;

                // Load pixels
                int pixIndex = 0;
                while (line > 0)
                {
                    // tokens can be of four types, and we will test for each below
                    if (_buffer[offset] == 0)
                    {
                        // if the token is a zero, then this is the end of the line,
                        // and we should decrement cur_line by one and forward the token
                        // marker by one.
                        //
                        // TODO: note that whole empty lines within the shape are
                        // communicated with a singular end token. not sure how to check
                        // that yet. :/

                        // prepare the offset jump
                        int jumpOffset = shapes[i].Height + 2 - line;
                        jumpOffset *= shapes[i].Width + 1;

                        // forward to the next line
                        offset++;
                        pixIndex = jumpOffset;
                        line--;
                    }
                    else if (_buffer[offset] == 1)
                    {
                        // if the token is a one, then this is a skip token.  the
                        // byte that follows the skip token is the argument byte,
                        // and we skip that many pixels ahead (implicitly filling with
                        // transparent values as we go).
                        offset++;              // we're done with the token byte
                        pixIndex += _buffer[offset]; // this is how far we have to skip
                        offset++;              // and now we're done with this byte
                    }
                    else if (_buffer[offset] % 2 == 0)
                    {
                        // if the token is non-zero and even, then this is a run token.
                        // run tokens have a pixel count and a pixel color associated
                        // with them.  we write that palette color to the bitmap count
                        // times, which allows for RLE compression within the SHP file.
                        int count = _buffer[offset] / 2;
                        offset++;
                        int pixel = _buffer[offset];
                        offset++;

                        // loop count times, storing the color to memory each time
                        while (count-- > 0)
                        {
                            int pixelX = pixIndex % textures[i].width;
                            int pixelY = (pixIndex - pixelX) / textures[i].width;
                            
                            textures[i].SetPixel(pixelX, textures[i].height - pixelY, _palette[pixel]);
                            pixIndex++;
                        }

                    }
                    else if (_buffer[offset] % 2 == 1)
                    {
                        // if the token is not one but is odd, then this is a string
                        // token.  we simply expand the count number of bytes that
                        // follow the initial token identifier into RGBA values and
                        // then write them sequentially to the bitmap.

                        int count = (_buffer[offset] - 1) / 2; // save the count to write
                        offset++;                      // advance the file marker

                        for (; count-- > 0;)
                        {
                            int pixelX = pixIndex % textures[i].width;
                            int pixelY = (pixIndex - pixelX) / textures[i].width;

                            // grab the color out of the file and then out of the
                            // palette, store to memory,
                            textures[i].SetPixel(pixelX, textures[i].height - pixelY, _palette[_buffer[offset]]);
                            // then advance the markers.
                            offset++;
                            pixIndex++;
                        }
                    }
                    else
                    {
                        Debug.LogError("Parsing error in SHP Parser.");
                    }
                }

                textures[i].Apply(false, true);
            }
            
            return textures;
        }

        public static Texture2D[] ParseShp(byte[] shpData, Color32[] palette)
        {
            ShpParser parser = new ShpParser(shpData, palette);
            return parser.Decode();
        }
    }
}