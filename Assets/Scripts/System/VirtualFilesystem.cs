using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Scripts.System.Compression;
using NAudio.Wave;
using UnityEngine;

namespace Assets.Scripts.System
{
    public class VirtualFilesystem
    {
        private byte[] _zfsMemory;

        private static VirtualFilesystem _instance;
        private readonly Game _game;

        public static VirtualFilesystem Instance
        {
            get { return _instance ?? (_instance = new VirtualFilesystem()); }
        }
        
        public string ZFSFilepath
        {
            get
            {
                return Path.Combine(_game.GamePath, "I76.ZFS");
            }
        }
        private readonly Dictionary<string, ZFSFileInfo> _files = new Dictionary<string, ZFSFileInfo>();

        // ReSharper disable once InconsistentNaming
        private class ZFSFileInfo
        {
            public string Filename { get; set; }
            public uint Offset { get; set; }
            public uint Id { get; set; }
            public uint Length { get; set; }
            public byte Compression { get; set; }
            public uint DecompressedLength { get; set; }
            public string ContainingPakFilename { get; set; }
        }

        private VirtualFilesystem()
        {
            _game = Game.Instance;
        }

        public void Init()
        {
            _zfsMemory = File.ReadAllBytes(ZFSFilepath);

            List<ZFSFileInfo> pixFiles = new List<ZFSFileInfo>();
            FastBinaryReader br = new FastBinaryReader(_zfsMemory);

            string magic = br.ReadCString(4);
            if (magic != "ZFSF")
                throw new Exception("Not an ZFS file");
            uint version = br.ReadUInt32();
            if (version != 1)
                throw new Exception("Only version 1 ZFS files supported");
            uint unk1 = br.ReadUInt32();
            uint numFilesInEachDirectory = br.ReadUInt32();
            uint numFilesTotal = br.ReadUInt32();
            uint unk2 = br.ReadUInt32();
            uint unk3 = br.ReadUInt32();

            while (true)
            {
                uint nextDirOffset = br.ReadUInt32();
                for (int fileIdx = 0; fileIdx < numFilesInEachDirectory; fileIdx++)
                {
                    if (_files.Count == numFilesTotal)
                        break;
                    ZFSFileInfo zfsFileInfo = new ZFSFileInfo();
                    zfsFileInfo.Filename = br.ReadCString(16).ToLower();
                    zfsFileInfo.Offset = br.ReadUInt32();
                    zfsFileInfo.Id = br.ReadUInt32();
                    zfsFileInfo.Length = br.ReadUInt32();
                    br.ReadUInt32(); // Unk
                    zfsFileInfo.Compression = br.ReadByte();
                    zfsFileInfo.DecompressedLength = br.ReadUInt16();
                    zfsFileInfo.DecompressedLength =
                        (uint)(br.ReadByte() << 16) |
                        zfsFileInfo.DecompressedLength; // Unk perhaps decompressed length is 3 bytes

                    _files.Add(zfsFileInfo.Filename, zfsFileInfo);
                    if (zfsFileInfo.Filename.EndsWithFast(".pix"))
                    {
                        pixFiles.Add(zfsFileInfo);
                    }
                }

                if (_files.Count == numFilesTotal)
                    break;
                br.Position = nextDirOffset;
            }

            // Parse all .pix-files
            //var pakContents = new Dictionary<string, ZFSFileInfo>();
            foreach (ZFSFileInfo pixFile in pixFiles)
            {
                using (FastBinaryReader sr = GetDataStream(pixFile))
                {
                    string l = sr.ReadLine();
                    int numFiles = int.Parse(l);
                    for (int i = 0; i < numFiles; i++)
                    {
                        string line = sr.ReadLine();
                        string[] splitted = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        string filename = splitted[0].ToLower();
                        uint offset = uint.Parse(splitted[1]);
                        uint length = uint.Parse(splitted[2]);

                        if (_files.ContainsKey(filename))
                            continue;
                        _files.Add(filename, new ZFSFileInfo
                        {
                            Filename = filename,
                            Offset = offset,
                            Length = length,
                            ContainingPakFilename = pixFile.Filename.Replace(".pix", ".pak")
                        });
                    }
                }
            }
        }

        internal AudioClip GetMusicClip(int trackNo)
        {
            string filePath = Path.Combine(_game.GamePath, "music", trackNo + ".mp3");
            if (!File.Exists(filePath))
                return null;

            var reader = new AudioFileReader(filePath);
            var data = new float[reader.Length];
            reader.Read(data, 0, data.Length);

            var audioClip = AudioClip.Create(filePath, data.Length, reader.WaveFormat.Channels, reader.WaveFormat.SampleRate, false);
            audioClip.SetData(data, 0);
            return audioClip;
        }

        public void ExtractAllMW2(string path)
        {
            string filename = Path.Combine(_game.GamePath, "DATABASE.MW2");
            using (BinaryReader br = new BinaryReader(new FileStream(filename, FileMode.Open)))
            {
                uint numFiles = br.ReadUInt32();
                uint[] offsets = new UInt32[numFiles];

                for (int i = 0; i < numFiles; i++)
                {
                    offsets[i] = br.ReadUInt32();
                }

                for (int i = 0; i < offsets.Length; i++)
                {
                    long length = (i == (offsets.Length - 1)) ? br.BaseStream.Length - offsets[i] : offsets[i + 1] - offsets[i];
                    br.BaseStream.Seek(offsets[i], SeekOrigin.Begin);
                    byte[] buffer = new byte[length];
                    int bytesRead = br.Read(buffer, 0, buffer.Length);
                    if (bytesRead != length)
                        throw new Exception("Wtf");
                    File.WriteAllBytes(Path.Combine(path, i.ToString()), buffer);
                }
            }
        }

        public void ExtractAll(string path)
        {
            foreach (KeyValuePair<string, ZFSFileInfo> zfsFileInfo in _files)
            {
                using (FastBinaryReader stream = GetDataStream(zfsFileInfo.Value))
                {
                    byte[] buf = stream.ReadBytes((int)zfsFileInfo.Value.Length);
                    File.WriteAllBytes(Path.Combine(path, zfsFileInfo.Value.Filename), buf);
                }
            }
        }
        
        public bool FileExists(string filename)
        {
            return _files.ContainsKey(filename.ToLower());
        }

        public AudioClip GetAudioClip(string filename)
        {
            if (!_files.TryGetValue(filename.ToLower(), out ZFSFileInfo fileInfo))
            {
                Debug.Log("File '" + filename + "' does not exist.");
                return null;
            }

            using (FastBinaryReader reader = GetFileStream(filename))
            {
                byte[] data = reader.ReadBytes((int)(reader.Length - reader.Position));
                return WavUtility.ToAudioClip(data, filename);
            }
        }

        public FastBinaryReader GetFileStream(string filename)
        {
            string replacementFilePath;
            if (filename.EndsWithFast(".msn") || filename.EndsWithFast(".ter"))
            {
                string missionDir = Path.Combine(_game.GamePath, "MISSIONS");
                if (!Directory.Exists(missionDir))
                {
                    missionDir = Path.Combine(_game.GamePath, "miss8");
                }
                replacementFilePath = Path.Combine(missionDir, filename);
            }
            else
            {
                replacementFilePath = Path.Combine(Path.Combine(_game.GamePath, "ADDON"), filename);
            }

            if (File.Exists(replacementFilePath))
            {
                byte[] data = File.ReadAllBytes(replacementFilePath);
                return new FastBinaryReader(data);
            }

            if (_files.TryGetValue(filename.ToLower(), out ZFSFileInfo fileInfo))
            {
                return GetDataStream(fileInfo);
            }
            
            return null;
        }
        
        private FastBinaryReader GetDataStream(ZFSFileInfo fileInfo)
        {
            ZFSFileInfo originalFile = null;
            if (fileInfo.ContainingPakFilename != null)
            {
                originalFile = fileInfo;
                fileInfo = _files[fileInfo.ContainingPakFilename];
            }

            if (fileInfo.Compression == 0)
            {
                FastBinaryReader br = new FastBinaryReader(_zfsMemory)
                {
                    Position = fileInfo.Offset,
                    Length = (int)(fileInfo.Offset + fileInfo.Length)
                };

                if (originalFile != null)
                {
                    br.Position += originalFile.Offset;
                    br.Length = (int)(br.Position + originalFile.Length);
                }

                return br;
            }

            CompressionAlgorithm compressionAlgorithm;
            if (fileInfo.Compression == 2)
                compressionAlgorithm = CompressionAlgorithm.LZO1X;
            else if (fileInfo.Compression == 4)
                compressionAlgorithm = CompressionAlgorithm.LZO1Y;
            else
                throw new Exception("Unknown compression " + fileInfo.Compression);

            byte[] fileData = new byte[fileInfo.Length];
            Buffer.BlockCopy(_zfsMemory, (int)fileInfo.Offset, fileData, 0, fileData.Length);

            byte[] decompressedData = new byte[fileInfo.DecompressedLength];
            uint length = LZO.Decompress(fileData, decompressedData, fileInfo.DecompressedLength, compressionAlgorithm);
            if (length != fileInfo.DecompressedLength)
            {
                throw new Exception("Decompressed length does not match expected decompressed length");
            }
            
            FastBinaryReader reader = new FastBinaryReader(decompressedData)
            {
                Position = 0,
                Length = decompressedData.Length
            };

            if (originalFile != null)
            {
                reader.Position = originalFile.Offset;
                reader.Length = (int)(originalFile.Offset + originalFile.Length);
            }

            return reader;
        }

        public IEnumerable<string> FindAllWithExtension(string extension)
        {
            return _files.Keys.Where(key => key.EndsWithFast(extension));
        }
    }
}
