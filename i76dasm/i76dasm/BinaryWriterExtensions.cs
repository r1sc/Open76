using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.System
{
    public static class BinaryWriterExtensions
    {
        public static void WriteCString(this BinaryWriter bw, string str, int totalLength)
        {
            var chrs = Encoding.ASCII.GetBytes(str.PadRight(totalLength, '\0'));
            if (chrs.Length > totalLength)
                throw new Exception("What");
            bw.Write(chrs);
        }
    }
}
