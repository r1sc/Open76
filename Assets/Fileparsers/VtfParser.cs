using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Fileparsers
{
    public class Vtf
    {
        public string VdfFilename { get; set; }
        public string PaintSchemeName { get; set; }
        public string[] Tmts { get; set; }
        public string[] Maps { get; set; }
    }

    class VtfParser
    {
        public static Vtf ParseVtf(string filename)
        {
            var vtf = new Vtf();
            using (var br = new Bwd2Reader(filename))
            {
                br.FindNext("VTFC");

                vtf.VdfFilename = br.ReadCString(13);
                vtf.PaintSchemeName = br.ReadCString(16);
                vtf.Tmts = new string[78];
                for (int i = 0; i < vtf.Tmts.Length; i++)
                {
                    vtf.Tmts[i] = br.ReadCString(13);
                }
                vtf.Maps = new string[13];
                for (int i = 0; i < vtf.Maps.Length; i++)
                {
                    vtf.Maps[i] = br.ReadCString(13).Replace(".map", "");
                }

            }

            foreach (var tmtFilename in vtf.Tmts)
            {
                
            }

            return vtf;
        }
    }
}
