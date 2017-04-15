using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Fileparsers
{
    public class Vdf
    {
        public string Name { get; set; }
    }

    public class VdfParser
    {
        public static void ParseVdf(string filename)
        {
            using (var br = new Bwd2Reader(filename))
            {
                br.FindNext("VDFC");

                var vdf = new Vdf();
                vdf.Name = br.ReadCString(16);
            }
        }
    }
}
