using UnityEngine;

namespace Assets.Fileparsers
{
    public class Gdf
    {
        public string Name { get; set; }
        public int Fireproofing { get; set; }
        public int FireAmount { get; set; }
        public float FiringRate { get; set; }
        public int AmmoCount { get; set; }
        public string FireSpriteName { get; set; }
        public string SoundName { get; set; }
        public string EnabledSpriteName { get; set; }
        public string DisabledSpriteName { get; set; }
        public SdfPart[] Parts { get; set; }
    }
    
    public class GdfParser
    {
        public static Gdf ParseGdf(string filename)
        {
            using (var br = new Bwd2Reader(filename))
            {
                var gdf = new Gdf();

                br.FindNext("REV");
                int rev = br.ReadInt32();

                br.FindNext("GDFC");
                {
                    gdf.Name = br.ReadCString(16);
                    int unk1 = br.ReadInt32();
                    int unk2 = br.ReadInt32();
                    float unk3 = br.ReadSingle();
                    float unk4 = br.ReadSingle();
                    float unk5 = br.ReadSingle();
                    br.ReadBytes(8);
                    int unk6 = br.ReadInt32();
                    gdf.Fireproofing = br.ReadInt32();
                    float unk7 = br.ReadSingle();
                    string unk8 = br.ReadCString(13);
                    br.ReadBytes(9);
                    gdf.FiringRate = 1f / br.ReadSingle();
                    gdf.FireAmount = br.ReadInt32();
                    float unk9 = br.ReadSingle();
                    float unk10 = br.ReadSingle();
                    gdf.AmmoCount = br.ReadInt32();
                    float unk11 = br.ReadSingle();

                    gdf.FireSpriteName = br.ReadCString(13);
                    gdf.SoundName = br.ReadCString(13);
                    if (rev == 8)
                    {
                        int unk12 = br.ReadInt32(); // Always 1?
                        gdf.EnabledSpriteName = br.ReadCString(16);
                        gdf.DisabledSpriteName = br.ReadCString(16);
                    }
                }

                br.FindNext("GPOF"); // 192 bytes
                for (int i = 0; i < 48; ++i)
                {
                    br.ReadSingle();
                }
                
                br.FindNext("GGEO"); // 4 bytes
                {
                    int numParts = br.ReadInt32();
                    gdf.Parts = new SdfPart[numParts];
                    for (int i = 0; i < numParts; ++i)
                    {
                        SdfPart sdfPart = new SdfPart
                        {
                            Name = br.ReadCString(8),
                            Right = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                            Up = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                            Forward = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                            Position = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                            ParentName = br.ReadCString(8)
                        };

                        gdf.Parts[i] = sdfPart;
                        br.Position += 56;
                    }
                }
                
                br.FindNext("ORDF"); // 133 bytes


                br.FindNext("OGEO"); // 104 bytes

                return gdf;
            }
        }
    }
}
