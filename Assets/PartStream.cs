using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets
{
    public class PartStream : Stream
    {
        private static readonly object LockObject = new object();

        private readonly Stream _baseStream;
        private readonly long _startOffset;
        private readonly long _length;

        public PartStream(Stream baseStream, long startOffset, long length)
        {
            _baseStream = baseStream;
            _startOffset = startOffset;
            _length = length;
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (LockObject)
            {
                var oldPos = _baseStream.Position;
                _baseStream.Position = _startOffset + Position;
                var bytesRead = _baseStream.Read(buffer, offset, count);
                _baseStream.Position = oldPos;
                Position += bytesRead;
                return bytesRead;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = _startOffset + offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = _startOffset + _length - offset;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("origin", origin, null);
            }
            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return _length; }
        }

        public override long Position { get; set; }
    }
}
