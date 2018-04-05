using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Assets.System.Compression
{
    public static class LZO
    {
        [DllImport("lzo2")]
        private static extern int lzo1x_decompress(byte[] src, uint src_len, byte[] dst, out uint dst_len, IntPtr wrkmem /* NOT USED */ );
        [DllImport("lzo2")]
        private static extern int lzo1y_decompress(byte[] src, uint src_len, byte[] dst, out uint dst_len, IntPtr wrkmem /* NOT USED */ );


        public static uint Decompress(byte[] compressedData, byte[] decompressedData, uint decompresedLength, CompressionAlgorithm algorithm)
        {
            uint realDecompressedLength;
            switch (algorithm)
            {
                case CompressionAlgorithm.LZO1X:
                    if (lzo1x_decompress(compressedData, (uint)compressedData.Length, decompressedData, out realDecompressedLength, IntPtr.Zero) != 0)
                        throw new Exception("Failure decompressing data");
                    break;
                case CompressionAlgorithm.LZO1Y:
                    if (lzo1y_decompress(compressedData, (uint)compressedData.Length, decompressedData, out realDecompressedLength, IntPtr.Zero) != 0)
                        throw new Exception("Failure decompressing data");
                    break;
                default:
                    throw new ArgumentOutOfRangeException("algorithm");
            }
            return realDecompressedLength;
        }
    }
}
