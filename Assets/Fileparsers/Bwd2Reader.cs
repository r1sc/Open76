using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Assets.Fileparsers
{
    public sealed class Bwd2Reader : BinaryReader
    {
        public class Tag
        {
            public string Name { get; set; }
            public long DataPosition { get; set; }
            public uint DataLength { get; set; }
            public Tag Next { get; set; }
        }

        private readonly Tag _root;
        public Tag Current;
        
        public Bwd2Reader(Stream stream) : base(stream)
        {
            while (BaseStream.Position < BaseStream.Length)
            {
                var tagName = new string(ReadChars(4)).Replace("\0", "");
                var dataLength = ReadUInt32() - 8;
                var tag = new Tag { Name = tagName, DataPosition = BaseStream.Position, DataLength = dataLength };
                if (_root == null)
                {
                    Current = _root = tag;
                }
                else
                {
                    Current.Next = tag;
                    Current = tag;
                }
                BaseStream.Seek(dataLength, SeekOrigin.Current);
            }
            Current = _root;
        }

        public Bwd2Reader(Bwd2Reader parentReader) : this(new PartStream(parentReader.BaseStream, parentReader.Current.DataPosition, parentReader.Current.DataLength))
        {
        }

        public Bwd2Reader(string path) : this(VirtualFilesystem.Instance.GetFileStream(path))
        {
        }

        public void FindNext(string recordName)
        {
            while (Current != null)
            {
                if (Current.Name == recordName)
                {
                    BaseStream.Position = Current.DataPosition;
                    break;
                }
                Current = Current.Next;
            }
        }

        public void Next()
        {
            Current = Current.Next;
            if (Current == null)
            {
                throw new Exception("EOF");
            }
            BaseStream.Position = Current.DataPosition;
        }
    }
}
