using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Assets.Scripts.System.Fileparsers
{
    public static class Mw2Parser
    {
        private struct Mw2
        {
            public int EntryCount { get; set; }
            public int[] Offsets { get; set; }
            public long[] Sizes { get; set; }
        }

        private static Mw2? _mw2;
        private static readonly string FilePath = Path.Combine(Game.Instance.GamePath, "DATABASE.MW2");
        private static readonly int BackgroundCount = Enum.GetNames(typeof(Background)).Length;
        private static readonly Dictionary<Background, Color32[]> BackgroundPalettes = new Dictionary<Background, Color32[]>();

        private enum DataType
        {
            Shp, // One or more images - palette data somewhat missing?
            Wav, // Audio
            Efa, // Compressed PCX - used for background images
            Fnt, // Font file
            Credits, // Credits
            Unknown
        }

        private static DataType GetFileTypeFromContent(byte[] bytes)
        {
            if (bytes[0] == '1' && bytes[1] == '.' && bytes[2] == '1' && bytes[3] == '0')
            {
                return DataType.Shp; // One or more images - palette data somewhat missing?
            }

            if ((bytes[0] == 'R' || bytes[0] == 'P') && bytes[1] == 'I' && bytes[2] == 'F' && bytes[3] == 'F')
            {
                return DataType.Wav; // Audio
            }
            
            if (bytes[4] == 0xEF && bytes[5] == 0x0A && bytes[6] == 0x05 && bytes[7] == 0x01)
            {
                return DataType.Efa; // Compressed PCX - used for background images
            }
            
            if (bytes[0] == '1' && bytes[1] == '.' && bytes[2] == 0 && bytes[3] == 0)
            {
                return DataType.Fnt; // Font file?
            }
            
            if (bytes[0] == 'P' && bytes[1] == 'R' && bytes[2] == 'O' && bytes[3] == 'D')
            {
                return DataType.Credits; // Credits, all ASCII
            }

            return DataType.Unknown;
        }

        public enum Background
        {
            BuildRepair,
            ChassisConfiguration,
            Inventory,
            PartsCatalog,
            AutoMeleeSinglePlayer,
            OptionsMenu,
            AudioControl,
            ExitGame,
            GraphicDetail,
            PlayOptions,
            SaveBookmark,
            LoadBookmark,
            Blank,
            OldMenu,
            SalvageExitConfirm,
            ControlConfig,
            PlayerInfo,
            NetMeleeEntryForm,
            NetMeleeHostEvent,
            ModemMeleeHostEvent,
            ModemMeleeEntryForm,
            NullModemHostEvent,
            NullModemEntryForm,
            AutoMeleeEntryForm1,
            AutoMeleeEntryForm2,
            MainMenu,
            ChooseVehicle,
            Standings,
            SalvageRepairNotAvailable,
            InventoryNotAvailable,
            Unconscious,
            ModemSetup
        }
        
        public enum TextureSet
        {
            Unknown1,
            Unknown2,
            Unknown3,
            Dialogs,
            Engine1,
            Engine2,
            Engine3,
            Engine4,
            Chassis1,
            Chassis2,
            Chassis3,
            Chassis4,
            Damage1,
            Damage2,
            Damage3,
            Damage4,
            Unknown4,
            Unknown5,
            Unknown6,
            Unknown7,
            Unknown8,
            Unknown9,
            Unknown10,
            Unknown11,
            Unknown12,
            Weapons,
            Unknown13,
            CarsSide,
            Cars3D,
            MainMenu,
            Repair,
            ChooseVehicle
        }

        public static Texture2D GetBackground(Background background)
        {
            int index = (int)background;
            return GetItem<Texture2D>(index);
        }

        public static Texture2D[] GetTextureSet(TextureSet textureSet, Color32[] palette)
        {
            int index = BackgroundCount + (int)textureSet;
            return GetItem<Texture2D[]>(index, palette);
        }

        public static Color32[] GetBackgroundPalette(Background background)
        {
            if (!BackgroundPalettes.TryGetValue(background, out Color32[] palette))
            {
                Debug.LogError("Failed to retrieve palette for background ''. Perhaps the background hasn't been loaded yet?");
                return null;
            }

            BackgroundPalettes.Remove(background); // Clean up the memory reference.
            return palette;
        }

        public static string[] GetCredits()
        {
            Mw2 mw2 = GetMw2();
            int lastIndex = mw2.Offsets[mw2.EntryCount - 1];
            return GetItem<string[]>(lastIndex);
        }

        private static Mw2 GetMw2()
        {
            if (_mw2 != null)
            {
                return _mw2.Value;
            }

            using (FileStream fileStream = File.Open(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (BinaryReader reader = new BinaryReader(fileStream))
                {
                    int entryCount = reader.ReadInt32();
                    int[] offsets = new int[entryCount];
                    long[] sizes = new long[entryCount];
                    for (int i = 0; i < entryCount; ++i)
                    {
                        offsets[i] = reader.ReadInt32();
                    }

                    for (int i = 0; i < entryCount; ++i)
                    {
                        sizes[i] = i == entryCount - 1
                            ? reader.BaseStream.Length - offsets[i]
                            : offsets[i + 1] - offsets[i];
                    }

                    _mw2 = new Mw2
                    {
                        EntryCount = entryCount,
                        Offsets = offsets,
                        Sizes = sizes
                    };
                }
            }

            return _mw2.Value;
        }

        public static byte[] GetBytes(int index)
        {
            Mw2 mw2 = GetMw2();

            using (FileStream fileStream = File.Open(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (BinaryReader reader = new BinaryReader(fileStream))
                {
                    int offset = mw2.Offsets[index];
                    long size = mw2.Sizes[index];
                    reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                    return reader.ReadBytes((int) size);
                }
            }
        }

        private static T GetItem<T>(int index, Color32[] palette = null) where T : class
        {
            Mw2 mw2 = GetMw2();

            if (index < 0 || index >= mw2.EntryCount)
            {
                Debug.LogError($"Index '{index}' is not in range of entry count ({mw2.EntryCount}).");
                return default;
            }

            using (FileStream fileStream = File.Open(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (BinaryReader reader = new BinaryReader(fileStream))
                {
                    int offset = mw2.Offsets[index];
                    long size = mw2.Sizes[index];
                    reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                    byte[] data = reader.ReadBytes((int)size);

                    DataType dataType = GetFileTypeFromContent(data);
                    if (dataType == DataType.Unknown)
                    {
                        Debug.LogError($"Unknown data found at MW2 database index '{index}'.");
                        return default;
                    }

                    if (typeof(T) == typeof(Texture2D) || typeof(T) == typeof(Texture2D[]))
                    {
                        if (dataType == DataType.Efa)
                        {
                            byte[] pcxData = EfaParser.ParseEfa(data);
                            Texture2D texture = PcxParser.ParsePcx(pcxData, out Color32[] outPalette);
                            if (!BackgroundPalettes.ContainsKey((Background) index))
                            {
                                BackgroundPalettes.Add((Background) index, outPalette);
                            }

                            return texture as T;
                        }
                        if (dataType == DataType.Shp)
                        {
                            return ShpParser.ParseShp(data, palette) as T;
                        }
                        if (dataType == DataType.Fnt)
                        {
                            return FntParser.ParseFnt(data) as T; // Doesn't really work.
                        }

                        Debug.LogError($"Data type expected was 'Efa', 'Shp' or 'Fnt'. Instead found data type of '{dataType.ToString()}' at index '{index}'.");
                        return default;
                    }

                    if (typeof(T) == typeof(string[]))
                    {
                        if (dataType != DataType.Credits)
                        {
                            Debug.LogError($"Data type expected was 'Credits', instead found data type of '{dataType.ToString()}' at index '{index}'.");
                            return default;
                        }

                        string text = Convert.ToString(data);
                        return text.Split('\n') as T;
                    }

                    Debug.LogError("Invalid data type requested from MW2 database.");
                    return default;
                }
            }
        }
    }
}
