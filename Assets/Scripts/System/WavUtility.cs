using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class WavUtility
{
    private enum ChunkType { Unknown, Fmt, Fact, Data }

    private static int Scan32Bits(byte[] source, int offset = 0)
    {
        return source[offset] | source[offset + 1] << 8 | source[offset + 2] << 16 | source[offset + 3] << 24;
    }

    private static int Scan16Bits(byte[] source, int offset = 0)
    {
        return source[offset] | source[offset + 1] << 8;
    }

    private abstract class Chunk
    {
        public ChunkType Ident = ChunkType.Unknown;
        protected int ByteCount;
    }

    private class FmtChunk : Chunk
    {
        private readonly int _channels;
        private readonly int _samplesPerSec;
        private readonly int _bitsPerSample;
        private readonly int _significantBits;

        public int Frequency { get { return _samplesPerSec; } }
        public int Channels { get { return _channels; } }
        public int BytesPerSample { get { return _bitsPerSample / 8 + ((_bitsPerSample % 8) > 0 ? 1 : 0); } }
        public int BitsPerSample
        {
            get
            {
                if (_significantBits > 0)
                    return _significantBits;
                return _bitsPerSample;
            }
        }

        public FmtChunk(byte[] buffer)
        {
            int size = buffer.Length;

            // if the length is 18 then buffer 16,17 should be 00 00 (I don't bother checking)
            if (size != 16 && size != 18 && size != 40)
            {
                return;
            }

            int formatCode = Scan16Bits(buffer);
            _channels = Scan16Bits(buffer, 2);
            _samplesPerSec = Scan32Bits(buffer, 4);
            _bitsPerSample = Scan16Bits(buffer, 14);

            if (formatCode == 0xfffe) // EXTENSIBLE
            {
                if (size != 40)
                {
                    return;
                }

                _significantBits = Scan16Bits(buffer, 18);
            }

            Ident = ChunkType.Fmt;
            ByteCount = size;
        }
    }

    private class DataChunk : Chunk
    {
        private readonly byte[] _samples;

        public DataChunk(byte[] buffer)
        {
            Ident = ChunkType.Data;
            ByteCount = buffer.Length;
            _samples = buffer;
        }

        /*public float[] GetAudioData(FmtChunk format)
        {
            int samplesPerChannel = ByteCount / (format.BytesPerSample * format.Channels);
            if (format.Channels > 1)
            {
                Debug.LogError("Audio file has more than 1 channel, this is not supported.");
                return null;
            }

            if (format.BitsPerSample != 8)
            {
                Debug.LogError("Audio file has more than 8 bits per channel, this is not supported.");
                return null;
            }

            float[] data = new float[samplesPerChannel];
            int mask = (int)Math.Floor(Math.Pow(2, format.BitsPerSample)) - 1;
            int offset = 0;
            for (int index = 0; index < samplesPerChannel; ++index)
            {
                data[index] = (_samples[offset] & mask) / 255f;
                offset += format.BytesPerSample;
                ++index;
            }

            return data;
        }*/

        public float[] GetAudioData(FmtChunk format)
        {
            if (format.Channels > 1)
            {
                Debug.LogError("More than 1 audio channel not implemented.");
                return null;
            }

            if (format.BitsPerSample != 8)
            {
                Debug.LogError("Only 8-bit audio is implemented.");
                return null;
            }

            int samplesPerChannel = ByteCount / format.BytesPerSample;

            float[] data = new float[samplesPerChannel];
            int mask = (int)Math.Floor(Math.Pow(2, format.BitsPerSample)) - 1;

            int offset = 0;
            for (int index = 0; index < samplesPerChannel; ++index)
            {
                data[index] = (_samples[offset] & mask) / 255f;
                offset += format.BytesPerSample;
            }

            return data;
        }
    }

    public static AudioClip ToAudioClip(byte[] audioData, string fileName)
    {
        using (MemoryStream fs = new MemoryStream(audioData))
        {
            using (BinaryReader reader = new BinaryReader(fs))
            {
                GetChunk(fs, reader); // RIFF chunk
                FmtChunk formatChunk = (FmtChunk)GetChunk(fs, reader);

                while (fs.Position < fs.Length)
                {
                    Chunk chunk = GetChunk(fs, reader);
                    if (chunk.Ident == ChunkType.Data)
                    {
                        DataChunk dc = (DataChunk)chunk;
                        float[] data = dc.GetAudioData(formatChunk);
                        AudioClip clip = AudioClip.Create(fileName, data.Length, 1, formatChunk.Frequency, false);
                        clip.SetData(data, 0);
                        return clip;
                    }
                }
            }
        }

        return null;
    }

    private static Chunk GetChunk(MemoryStream stream, BinaryReader reader)
    {
        byte[] buffer = reader.ReadBytes(8);
        if (buffer.Length != 8)
        {
            return null;
        }

        string prefix = new string(Encoding.ASCII.GetChars(buffer, 0, 4));
        int size = Scan32Bits(buffer, 4);

        if (size + stream.Position > stream.Length) // skip if there isn't enough data
        {
            return null;
        }

        if (string.CompareOrdinal(prefix, "RIFF") == 0)
        {
            if (size < 4)
            {
                return null;
            }

            reader.ReadBytes(4);
        }
        else if (string.CompareOrdinal(prefix, "fmt ") == 0)
        {
            if (size < 16)
            {
                return null;
            }

            buffer = reader.ReadBytes(size);
            if (buffer.Length == size)
            {
                return new FmtChunk(buffer);
            }

        }
        else if (string.CompareOrdinal(prefix, "fact") == 0)
        {
            if (size < 4)
            {
                return null;
            }

            reader.ReadBytes(4);
        }
        else if (string.CompareOrdinal(prefix, "data") == 0)
        {
            if (size == 0)
            {
                return null;
            }

            buffer = reader.ReadBytes(size);
            if ((size & 1) != 0) // odd length?
            {
                if (stream.Position < stream.Length)
                {
                    reader.ReadByte();
                }
            }
            if (buffer.Length == size)
            {
                return new DataChunk(buffer);
            }
        }
        else
        {
            if (size == 0)
            {
                return null;
            }

            reader.ReadBytes(size);
            if ((size & 1) != 0) // odd length?
            {
                if (stream.Position < stream.Length)
                {
                    reader.ReadByte();
                }
            }
        }

        return null;
    }
}