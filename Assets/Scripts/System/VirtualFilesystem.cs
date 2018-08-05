using Assets.System.Compression;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.System
{
    public class VirtualFilesystem
    {
        private static VirtualFilesystem _instance;

        public static VirtualFilesystem Instance
        {
            get { return _instance ?? (_instance = new VirtualFilesystem()); }
        }

        public string Gamepath { get; private set; }
        public string ZFSFilepath
        {
            get
            {
                return Path.Combine(Gamepath, "I76.ZFS");
            }
        }
        private readonly Dictionary<string, ZFSFileInfo> _files = new Dictionary<string, ZFSFileInfo>();

        // ReSharper disable once InconsistentNaming
        class ZFSFileInfo
        {
            public string Filename { get; set; }
            public uint Offset { get; set; }
            public uint Id { get; set; }
            public uint Length { get; set; }
            public byte Compression { get; set; }
            public uint DecompressedLength { get; set; }
            public string ContainingPakFilename { get; set; }
        }

        public void Init(string gamepath)
        {
            Gamepath = gamepath;

            var pixFiles = new List<string>();
            using (var br = new BinaryReader(new FileStream(ZFSFilepath, FileMode.Open)))
            {
                var magic = br.ReadCString(4);
                if (magic != "ZFSF")
                    throw new Exception("Not an ZFS file");
                var version = br.ReadUInt32();
                if (version != 1)
                    throw new Exception("Only version 1 ZFS files supported");
                var unk1 = br.ReadUInt32();
                var numFilesInEachDirectory = br.ReadUInt32();
                var numFilesTotal = br.ReadUInt32();
                var unk2 = br.ReadUInt32();
                var unk3 = br.ReadUInt32();

                while (true)
                {
                    var nextDirOffset = br.ReadUInt32();
                    for (int fileIdx = 0; fileIdx < numFilesInEachDirectory; fileIdx++)
                    {
                        if (_files.Count == numFilesTotal)
                            break;
                        var zfsFileInfo = new ZFSFileInfo();
                        zfsFileInfo.Filename = br.ReadCString(16).ToLower();
                        zfsFileInfo.Offset = br.ReadUInt32();
                        zfsFileInfo.Id = br.ReadUInt32();
                        zfsFileInfo.Length = br.ReadUInt32();
                        br.ReadUInt32(); // Unk
                        zfsFileInfo.Compression = br.ReadByte();
                        zfsFileInfo.DecompressedLength = br.ReadUInt16();
                        zfsFileInfo.DecompressedLength = (uint)(br.ReadByte() << 16) | zfsFileInfo.DecompressedLength; // Unk perhaps decompressed length is 3 bytes

                        _files.Add(zfsFileInfo.Filename, zfsFileInfo);
                        if (zfsFileInfo.Filename.EndsWith(".pix"))
                        {
                            pixFiles.Add(zfsFileInfo.Filename);
                        }
                    }
                    if (_files.Count == numFilesTotal)
                        break;
                    br.BaseStream.Position = nextDirOffset;
                }
            }


            // Parse all .pix-files
            //var pakContents = new Dictionary<string, ZFSFileInfo>();
            foreach (var pixFile in pixFiles)
            {
                using (var sr = new StreamReader(GetFileStream(pixFile), Encoding.ASCII))
                {
                    var l = sr.ReadLine();
                    var numFiles = int.Parse(l);
                    for (var i = 0; i < numFiles; i++)
                    {
                        var line = sr.ReadLine();
                        var splitted = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        var filename = splitted[0].ToLower();
                        var offset = uint.Parse(splitted[1]);
                        var length = uint.Parse(splitted[2]);

                        if (_files.ContainsKey(filename))
                            continue;
                        _files.Add(filename, new ZFSFileInfo
                        {
                            Filename = filename,
                            Offset = offset,
                            Length = length,
                            ContainingPakFilename = pixFile.Replace(".pix", ".pak")
                        });
                    }
                }
            }
        }

        public void ExtractAllMW2(string path)
        {
            var filename = Path.Combine(Gamepath, "DATABASE.MW2");
            using (var br = new BinaryReader(new FileStream(filename, FileMode.Open)))
            {
                var numFiles = br.ReadUInt32();
                var offsets = new UInt32[numFiles];

                for (int i = 0; i < numFiles; i++)
                {
                    offsets[i] = br.ReadUInt32();
                }

                for (int i = 0; i < offsets.Length; i++)
                {
                    var length = (i == (offsets.Length - 1)) ? br.BaseStream.Length - offsets[i] : offsets[i + 1] - offsets[i];
                    br.BaseStream.Seek(offsets[i], SeekOrigin.Begin);
                    var buffer = new byte[length];
                    var bytesRead = br.Read(buffer, 0, buffer.Length);
                    if (bytesRead != length)
                        throw new Exception("Wtf");
                    File.WriteAllBytes(Path.Combine(path, i.ToString()), buffer);
                }
            }
        }

        public void ExtractAll(string path)
        {
            foreach (var zfsFileInfo in _files)
            {
                using (var stream = GetFileStream(zfsFileInfo.Key))
                {
                    var buf = new byte[zfsFileInfo.Value.Length];
                    stream.Read(buf, 0, buf.Length);
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
            if (!FileExists(filename))
            {
                Debug.Log("File '" + filename + "' does not exist.");
                return null;
            }

            if (!filename.ToLower().EndsWith("wav"))
            {
                Debug.Log("File is not an audio file.");
                return null;
            }

            using (Stream stream = GetFileStream(filename))
            {
                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, (int)stream.Length);
                return WavUtility.ToAudioClip(data, 0, filename);
            }
        }

        public Stream GetFileStream(string filename)
        {
            if (Gamepath == null)
                throw new Exception("Resource manager not initialized");

            string replacementFilePath;
            if (filename.EndsWith(".msn") || filename.EndsWith(".ter"))
            {
                string missionDir = Path.Combine(Gamepath, "MISSIONS");
                if (!Directory.Exists(missionDir))
                {
                    missionDir = Path.Combine(Gamepath, "miss8");
                }
                replacementFilePath = Path.Combine(missionDir, filename);
            }
            else
                replacementFilePath = Path.Combine(Path.Combine(Gamepath, "ADDON"), filename);
            if (File.Exists(replacementFilePath))
            {
                return new FileStream(replacementFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }

            if (!_files.ContainsKey(filename.ToLower()))
                throw new Exception("File not found in ZFS: " + filename);
            var file = _files[filename.ToLower()];
            if (file.ContainingPakFilename != null)
            {
                var pakFile = _files[file.ContainingPakFilename];
                var pakStream = GetDataStream(ZFSFilepath, pakFile.Offset, pakFile.Length, pakFile);
                return new PartStream(pakStream, file.Offset, file.Length);
            }
            return GetDataStream(ZFSFilepath, file.Offset, file.Length, file);
        }

        private Stream GetDataStream(string path, long offset, long length, ZFSFileInfo fileInfo)
        {
            if (fileInfo.Compression == 0)
                return new PartStream(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read), offset, length);

            CompressionAlgorithm compressionAlgorithm;
            if (fileInfo.Compression == 2)
                compressionAlgorithm = CompressionAlgorithm.LZO1X;
            else if (fileInfo.Compression == 4)
                compressionAlgorithm = CompressionAlgorithm.LZO1Y;
            else
                throw new Exception("Unknown compression " + fileInfo.Compression);

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fs.Position = offset;
                var fileData = new byte[length];
                fs.Read(fileData, 0, fileData.Length);
                return new CompressedStream(fileData, fileInfo.DecompressedLength, compressionAlgorithm);
            }
        }

        public IEnumerable<string> FindAllWithExtension(string extension)
        {
            return _files.Keys.Where(key => key.EndsWith(extension));
        }
    }
}
