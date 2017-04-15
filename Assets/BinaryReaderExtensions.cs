using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets
{
    public static class BinaryReaderExtensions
    {
        public static string ReadCString(this BinaryReader br, int maxLength)
        {
            var chrs = br.ReadBytes(maxLength);
            for (int i = 0; i < chrs.Length; i++)
            {
                if (chrs[i] == 0)
                    return Encoding.ASCII.GetString(chrs, 0, i);
            }
            return Encoding.ASCII.GetString(chrs);
        }
    }
}
