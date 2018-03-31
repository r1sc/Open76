using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets
{
    public class VirtualFilesystem
    {
        private static VirtualFilesystem _instance;

        public static VirtualFilesystem Instance
        {
            get { return _instance ?? (_instance = new VirtualFilesystem()); }
        }

        public string Gamepath { get; private set; }
        private BinaryReader _br;
        private readonly Dictionary<string, ZFSFileInfo> _files = new Dictionary<string, ZFSFileInfo>();

        // ReSharper disable once InconsistentNaming
        class ZFSFileInfo
        {
            public string Filename { get; set; }
            public uint Offset { get; set; }
            public uint Id { get; set; }
            public uint Length { get; set; }
            public uint Hashunk { get; set; }
            public string ContainingPakFilename { get; set; }
        }

        public void Init(string gamepath)
        {
            var zfsPath = Path.Combine(gamepath, "I76.ZFS");
            _br = new BinaryReader(new FileStream(zfsPath, FileMode.Open));

            var magic = _br.ReadCString(4);
            if (magic != "ZFSF")
                throw new Exception("Not an ZFS file");
            var version = _br.ReadUInt32();
            if (version != 1)
                throw new Exception("Only version 1 ZFS files supported");
            var unk1 = _br.ReadUInt32();
            var numFilesInEachDirectory = _br.ReadUInt32();
            var numFilesTotal = _br.ReadUInt32();
            var unk2 = _br.ReadUInt32();
            var unk3 = _br.ReadUInt32();

            var pixFiles = new List<string>();
            while (true)
            {
                var nextDirOffset = _br.ReadUInt32();
                for (int fileIdx = 0; fileIdx < numFilesInEachDirectory; fileIdx++)
                {
                    if (_files.Count == numFilesTotal)
                        break;
                    var zfsFileInfo = new ZFSFileInfo();
                    zfsFileInfo.Filename = _br.ReadCString(16).ToLower();
                    zfsFileInfo.Offset = _br.ReadUInt32();
                    zfsFileInfo.Id = _br.ReadUInt32();
                    zfsFileInfo.Length = _br.ReadUInt32();
                    zfsFileInfo.Hashunk = _br.ReadUInt32();
                    var nullmarker = _br.ReadUInt32();
                    _files.Add(zfsFileInfo.Filename, zfsFileInfo);
                    if (zfsFileInfo.Filename.EndsWith(".pix"))
                    {
                        pixFiles.Add(zfsFileInfo.Filename);
                    }
                }
                if (_files.Count == numFilesTotal)
                    break;
                _br.BaseStream.Position = nextDirOffset;
            }

            Gamepath = gamepath;

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

        public Stream GetFileStream(string filename)
        {
            if (Gamepath == null)
                throw new Exception("Resource manager not initialized");

            string replacementFilePath;
            if (filename.EndsWith(".msn") || filename.EndsWith(".ter"))
            {
                replacementFilePath = Path.Combine(Path.Combine(Gamepath, "MISSIONS"), filename);
            }
            else
                replacementFilePath = Path.Combine(Path.Combine(Gamepath, "ADDON"), filename);
            if (File.Exists(replacementFilePath))
            {
                return new FileStream(replacementFilePath, FileMode.Open);
            }

            if (!_files.ContainsKey(filename.ToLower()))
                throw new Exception("File not found in ZFS: " + filename);
            var file = _files[filename.ToLower()];
            if (file.ContainingPakFilename != null)
            {
                var pakFile = _files[file.ContainingPakFilename];
                return new PartStream(_br.BaseStream, pakFile.Offset + file.Offset, file.Length);
            }
            return new PartStream(_br.BaseStream, file.Offset, file.Length);
        }

        public IEnumerable<string> FindAllWithExtension(string extension)
        {
            return _files.Keys.Where(key => key.EndsWith(extension));
        }
    }
}
