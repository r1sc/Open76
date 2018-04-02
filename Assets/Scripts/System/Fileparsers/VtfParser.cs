using Assets.System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.Fileparsers
{
    public class Vtf
    {
        public string VdfFilename { get; set; }
        public string PaintSchemeName { get; set; }
        public string[] TmtFilenames { get; set; }
        public Dictionary<string, Tmt> Tmts { get; set; }
        public string[] Maps { get; set; }
    }

    public class Tmt
    {
        public List<string> TextureNames { get; set; }
    }

    class VtfParser
    {

        /*
            TMT's are referred in this order:
            Front, Back, Right, Left, Top, Under

            Given a filename of:
            1CP1FTFT

            decodes as 
            1CP = First cop car skin
            1 = LOD level
            FTFT = Front Front

            Sequence of tmt files:
            Front
            Mid
            Back
            Top

            Full list:

            FTFT
            FTBK
            FTRT
            FTLT
            FTTP
            FTUN

            MDFT
            MDBK
            MDRT
            MDLT
            MDTP
            MDUN

            BKFT
            BKBK
            BKRT
            BKLT
            BKTP
            BKUN

            TPFT
            TPBK
            TPRT
            TPLT
            TPTP
            TPUN
            */ 
         

        public static Vtf ParseVtf(string filename)
        {
            var vtf = new Vtf();
            using (var br = new Bwd2Reader(filename))
            {
                br.FindNext("VTFC");

                vtf.VdfFilename = br.ReadCString(13);
                vtf.PaintSchemeName = br.ReadCString(16);

                vtf.TmtFilenames = new string[78];
                for (int i = 0; i < vtf.TmtFilenames.Length; i++)
                {
                    vtf.TmtFilenames[i] = br.ReadCString(13);
                }

                vtf.Maps = new string[13];
                for (int i = 0; i < vtf.Maps.Length; i++)
                {
                    vtf.Maps[i] = br.ReadCString(13).Replace(".map", "");
                }

            }

            vtf.Tmts = new Dictionary<string, Tmt>();
            for (var tmtIdx = 0; tmtIdx < vtf.TmtFilenames.Length; tmtIdx++)
            {
                var tmtFilename = vtf.TmtFilenames[tmtIdx];
                if (tmtFilename != "NULL")
                {
                    using (var br = new BinaryReader(VirtualFilesystem.Instance.GetFileStream(tmtFilename)))
                    {
                        var one = br.ReadUInt32();
                        var zero1 = br.ReadUInt32();
                        var zero2 = br.ReadUInt32();
                        var zero3 = br.ReadUInt32();
                        var zero4 = br.ReadUInt32();
                        var two = br.ReadUInt32();
                        var two2 = br.ReadUInt32();
                        var four = br.ReadUInt32();
                        var zero5 = br.ReadUInt32();
                        var zero6 = br.ReadUInt32();
                        var tenFloat = br.ReadSingle();
                        var zero7 = br.ReadSingle();
                        var zero8 = br.ReadSingle();
                        var zero9 = br.ReadSingle();
                        var zero10 = br.ReadSingle();
                        var zero11 = br.ReadSingle();

                        var tmt = new Tmt
                        {
                            TextureNames = new List<string>()
                        };
                        while(br.BaseStream.Position < br.BaseStream.Length)
                        {
                            var textureName = br.ReadCString(8);
                            tmt.TextureNames.Add(textureName);
                        }
                        vtf.Tmts.Add(tmtFilename.Substring(3), tmt);
                    }
                }
            }

            return vtf;
        }
    }
}
