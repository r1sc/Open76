using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.System.Fileparsers
{
    public class Gpw
    {
        public AudioClip Clip { get; set; }
        public short AudioRange { get; set; }
    }

    public class GpwParser
    {
        private static readonly Dictionary<string, Gpw> GpwCache = new Dictionary<string, Gpw>();

        public static Gpw ParseGpw(string fileName)
        {
            if (GpwCache.TryGetValue(fileName, out Gpw gpw))
            {
                return gpw;
            }

            using (FastBinaryReader br = VirtualFilesystem.Instance.GetFileStream(fileName))
            {
                gpw = new Gpw();
                string header = br.ReadCString(4); // Always GAS0
                gpw.AudioRange = (short)br.ReadUInt16();
                ushort unk2 = br.ReadUInt16();
                int unk3 = br.ReadInt32();
                int unk4 = br.ReadInt32();
                int unk5 = br.ReadInt32();
                int unk6 = br.ReadInt32();
                int unk7 = br.ReadInt32();
                br.Position += 4; // Skip RIFF header
                int waveFileSize = br.ReadInt32() + 8; // Read total size of audio data from RIFF header
                br.Position -= 8; // Go back to RIFF header

                byte[] audioData = br.ReadBytes(waveFileSize);
                gpw.Clip = WavUtility.ToAudioClip(audioData, fileName);

                GpwCache.Add(fileName, gpw);
                return gpw;
            }
        }
    }
}
