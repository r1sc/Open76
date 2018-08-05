using System;
using System.Text;

namespace Assets.Scripts.System
{
    public class FastBinaryReader : IDisposable
    {
        private byte[] _data;
        private int _offset;

        public byte[] Data
        {
            get { return _data; }
        }

        public long Position
        {
            get { return (uint)_offset; }
            set { _offset = (int)value; }
        }

        public int Length { get; set; }

        public FastBinaryReader(byte[] data)
        {
            _data = data;
            Length = _data.Length;
        }

        public FastBinaryReader(byte[] data, long offset, uint length)
        {
            _data = data;
            _offset = (int)offset;
            Length = (int)(_offset + length);
        }

        public FastBinaryReader(FastBinaryReader parent)
        {
            _data = parent.Data;
            _offset = parent._offset;
            Length = parent.Length;
        }

        public string ReadLine()
        {
            int start = _offset;
            while (_data[_offset] != '\r' && _offset < _data.Length)
            {
                ++_offset;
            }

            char[] chars = new char[_offset - start];


            for (int i = start; i < _offset; ++i)
            {
                chars[i - start] = (char) _data[i];
            }

            _offset += 2;
            return new string(chars);
        }

        public string ReadCString(int maxLength)
        {
            byte[] chrs = new byte[maxLength];
            Buffer.BlockCopy(_data, _offset, chrs, 0, maxLength);
            _offset += maxLength;

            for (int i = 0; i < maxLength; i++)
            {
                chrs[i] = (byte)(chrs[i] & 0x7F);   // Skip high byte (old skool ascii)
            }
            for (int i = 0; i < maxLength; i++)
            {
                if (chrs[i] == 0)
                    return Encoding.ASCII.GetString(chrs, 0, i);
            }
            return Encoding.ASCII.GetString(chrs);
        }

        public byte ReadByte()
        {
            return _data[_offset++];
        }

        public byte[] ReadBytes(int count)
        {
            if (_offset + count > _data.Length)
            {
                count = _data.Length - _offset;
            }

            byte[] bytes = new byte[count];

            Buffer.BlockCopy(_data, _offset, bytes, 0, count);
            _offset += count;
            return bytes;
        }
        
        public int ReadInt32()
        {
            int val = _data[_offset] | (_data[_offset + 1] << 8) | (_data[_offset + 2] << 16) | (_data[_offset + 3] << 24);
            _offset += sizeof(int);
            return val;
        }

        public uint ReadUInt32()
        {
            uint val = (uint)(_data[_offset] | (_data[_offset + 1] << 8) | (_data[_offset + 2] << 16) | (_data[_offset + 3] << 24));
            _offset += sizeof(uint);
            return val;
        }

        public ushort ReadUInt16()
        {
            ushort val = (ushort)(_data[_offset] | (_data[_offset + 1] << 8));
            _offset += sizeof(ushort);
            return val;
        }
        
        public float ReadSingle()
        {
            float val = BitConverter.ToSingle(_data, _offset);
            _offset += sizeof(float);
            return val;
        }

        public virtual void Dispose()
        {
			// There isn't ~really~ anything to actually dispose.
			// Mainly left it here for cleaner code structure.
			_data = null;
			Length = 0;
			_offset = 0;
        }
    }
}
