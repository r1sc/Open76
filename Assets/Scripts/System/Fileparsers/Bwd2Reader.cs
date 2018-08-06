using Assets.System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Assets.Scripts.System;

namespace Assets.Fileparsers
{
    public sealed class Bwd2Reader : FastBinaryReader
    {
        public class Tag
        {
            public string Name { get; set; }
            public long DataPosition { get; set; }
            public uint DataLength { get; set; }
            public Tag Next { get; set; }
        }

        private bool _isChildReader = false;
        private readonly Tag _root;
        public Tag Current;
        
        public Bwd2Reader(FastBinaryReader reader) : base(new FastBinaryReader(reader))
        {
            while (Position < Length)
            {
                var tagName = ReadCString(4);
                var dataLength = ReadUInt32() - 8;
                var tag = new Tag { Name = tagName, DataPosition = Position, DataLength = dataLength };
                if (_root == null)
                {
                    Current = _root = tag;
                }
                else
                {
                    Current.Next = tag;
                    Current = tag;
                }

                Position += dataLength;
            }
            Current = _root;
        }

        public Bwd2Reader(Bwd2Reader parentReader) : this(new FastBinaryReader(parentReader.Data, parentReader.Current.DataPosition, parentReader.Current.DataLength))
        {
            _isChildReader = true;
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
                    Position = Current.DataPosition;
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
            Position = Current.DataPosition;
        }

        public override void Dispose()
        {
            if(!_isChildReader)
                base.Dispose();
        }
    }
}
