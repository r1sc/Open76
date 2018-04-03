using System;
using System.IO;

namespace Assets.System.Compression
{
    public class CompressedStream : Stream
    {
        private readonly byte[] _decompressedData;
        private readonly long _length;

        public CompressedStream(byte[] compressedData, uint decompressedLength, CompressionAlgorithm algorithm)
        {
            _decompressedData = new byte[decompressedLength];
            _length = LZO.Decompress(compressedData, _decompressedData, decompressedLength, algorithm);
            if (_length != decompressedLength)
                throw new Exception("Decompressed length does not match expected decompressed length");
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesRead = Math.Min(count, _length - Position);
            Array.Copy(_decompressedData, (int)Position, buffer, offset, bytesRead);
            Position += bytesRead;
            return (int)bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = _length - offset;
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
