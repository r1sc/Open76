using System.Collections.Generic;

namespace Assets.Scripts.System.Fileparsers
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

    internal class VtfParser
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
            Vtf vtf = new Vtf();
            using (Bwd2Reader br = new Bwd2Reader(filename))
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
            for (int tmtIdx = 0; tmtIdx < vtf.TmtFilenames.Length; tmtIdx++)
            {
                string tmtFilename = vtf.TmtFilenames[tmtIdx];
                if (tmtFilename != "NULL")
                {
                    using (Scripts.System.FastBinaryReader br = VirtualFilesystem.Instance.GetFileStream(tmtFilename))
                    {
                        uint one = br.ReadUInt32();
                        uint zero1 = br.ReadUInt32();
                        uint zero2 = br.ReadUInt32();
                        uint zero3 = br.ReadUInt32();
                        uint zero4 = br.ReadUInt32();
                        uint two = br.ReadUInt32();
                        uint two2 = br.ReadUInt32();
                        uint four = br.ReadUInt32();
                        uint zero5 = br.ReadUInt32();
                        uint zero6 = br.ReadUInt32();
                        float tenFloat = br.ReadSingle();
                        float zero7 = br.ReadSingle();
                        float zero8 = br.ReadSingle();
                        float zero9 = br.ReadSingle();
                        float zero10 = br.ReadSingle();
                        float zero11 = br.ReadSingle();

                        Tmt tmt = new Tmt
                        {
                            TextureNames = new List<string>()
                        };

                        while(br.Position < br.Length)
                        {
                            string textureName = br.ReadCString(8);
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
