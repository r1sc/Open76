using UnityEngine;

namespace Assets.Fileparsers
{
    public class Gdf
    {
        public string Name { get; set; }
        public string FireSpriteName { get; set; }
        public string SoundName { get; set; }
        public string EnabledSpriteName { get; set; }
        public string DisabledSpriteName { get; set; }
    }

    class GdfParser
    {
        public static Gdf ParseGdf(string filename)
        {
            using (var br = new Bwd2Reader(filename))
            {
                var gdf = new Gdf();

                br.FindNext("GDFC"); // 128 bytes
                gdf.Name = br.ReadCString(16);
                int unk1 = br.ReadInt32();
                int unk2 = br.ReadInt32();
                float unk3 = br.ReadSingle();
                float unk4 = br.ReadSingle();
                float unk5 = br.ReadSingle();
                br.ReadBytes(20);
                string unk6 = br.ReadCString(13);
                br.ReadBytes(33);
                gdf.FireSpriteName = br.ReadCString(13);
                gdf.SoundName = br.ReadCString(13);
                int unk7 = br.ReadInt32(); // Always 1?
                gdf.EnabledSpriteName = br.ReadCString(16);
                gdf.DisabledSpriteName = br.ReadCString(16);

                br.FindNext("GPOF"); // 192 bytes


                br.FindNext("GGEO"); // 4 bytes


                br.FindNext("ORDF"); // 133 bytes


                br.FindNext("OGEO"); // 104 bytes

                return gdf;
            }
        }
    }
}
