using UnityEngine;

namespace Assets.Scripts.System.Fileparsers
{
    public class ActPaletteParser
    {
        public static Color32[] ReadActPalette(string filename)
        {
            using (Scripts.System.FastBinaryReader br = VirtualFilesystem.Instance.GetFileStream(filename))
            {
                Color32[] colors = new Color32[256];
                for (int i = 0; i < 256; i++)
                {
                    byte r = br.ReadByte();
                    byte g = br.ReadByte();
                    byte b = br.ReadByte();
                    colors[i] = new Color32(r, g, b, 255);
                }
                return colors;
            }
        }
    }
}
