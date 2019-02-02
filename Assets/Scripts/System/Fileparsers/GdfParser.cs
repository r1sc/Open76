using UnityEngine;

namespace Assets.Scripts.System.Fileparsers
{
    public class Gdf
    {
        public string Name { get; set; }
        public int Health { get; set; }
        public int Damage { get; set; }
        public int FireAmount { get; set; }
        public float BurstRate { get; set; }
        public float BulletVelocity { get; set; }
        public float FiringRate { get; set; }
        public int WeaponGroup { get; set; }
        public int AmmoCount { get; set; }
        public float WeaponMass { get; set; }
        public string FireSpriteName { get; set; }
        public string SoundName { get; set; }
        public string EnabledSpriteName { get; set; }
        public string DisabledSpriteName { get; set; }
        public SdfPart[] TopParts { get; set; }
        public SdfPart[] SideParts { get; set; }
        public SdfPart[] InsideParts { get; set; }
        public SdfPart[] TurretParts { get; set; }
        public SdfPart Projectile { get; set; }
    }
    
    public class GdfParser
    {
        public static Gdf ParseGdf(string filename)
        {
            using (Bwd2Reader br = new Bwd2Reader(filename))
            {
                Gdf gdf = new Gdf();

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
                    float unk6 = br.ReadSingle();
                    br.ReadBytes(4);
                    gdf.Damage = br.ReadInt32();
                    gdf.Health = br.ReadInt32();
                    gdf.WeaponMass = br.ReadSingle();
                    string unk7 = br.ReadCString(12);
                    ushort unk8 = br.ReadUInt16();
                    float unk9 = br.ReadSingle();
                    gdf.BurstRate = 1f / br.ReadSingle();
                    gdf.FiringRate = 1f / br.ReadSingle();
                    gdf.FireAmount = br.ReadInt32();
                    gdf.BulletVelocity = br.ReadSingle();
                    gdf.WeaponGroup = br.ReadInt32();
                    gdf.AmmoCount = br.ReadInt32();
                    float unk10 = br.ReadSingle();

                    gdf.FireSpriteName = br.ReadCString(13);
                    gdf.SoundName = br.ReadCString(13);
                    if (rev == 8)
                    {
                        int unk11 = br.ReadInt32(); // Always 1?
                        gdf.EnabledSpriteName = br.ReadCString(16);
                        gdf.DisabledSpriteName = br.ReadCString(16);
                    }
                }

                br.FindNext("GPOF"); // 192 bytes

                SdfPart[] parts = new SdfPart[4];
                for (int i = 0; i < 4; ++i)
                {
                    SdfPart part = new SdfPart();
                    part.Right = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    part.Up = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    part.Forward = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    part.Position = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    parts[i] = part;
                }
                
                br.FindNext("GGEO"); // 4 bytes
                {
                    int numParts = br.ReadInt32();
                    if (numParts > 0)
                    {
                        int topPartsIndex = 0;
                        int sidePartsIndex = 0;
                        int turretPartsIndex = 0;
                        int insidePartsIndex = 0;

                        const int weaponSlots = 4;
                        int totalSlots = weaponSlots * numParts;
                        for (int i = 0; i < totalSlots; ++i)
                        {
                            string partName = br.ReadCString(8);
                            if (partName == "NULL")
                            {
                                br.Position += 92;
                            }
                            else
                            {
                                SdfPart sdfPart = new SdfPart
                                {
                                    Name = partName,
                                    Right = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                                    Up = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                                    Forward = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                                    Position = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                                    ParentName = br.ReadCString(8)
                                };

                                char partType = partName[3];
                                switch (partType)
                                {
                                    case 'P':
                                        if (gdf.TopParts == null)
                                        {
                                            gdf.TopParts = new SdfPart[numParts];
                                        }

                                        gdf.TopParts[topPartsIndex++] = sdfPart;
                                        break;
                                    case 'S':
                                        if (gdf.SideParts == null)
                                        {
                                            gdf.SideParts = new SdfPart[numParts];
                                        }

                                        gdf.SideParts[sidePartsIndex++] = sdfPart;
                                        break;
                                    case 'T':
                                        if (gdf.TurretParts == null)
                                        {
                                            gdf.TurretParts = new SdfPart[numParts];
                                        }

                                        gdf.TurretParts[turretPartsIndex++] = sdfPart;
                                        break;
                                    case 'I':
                                        if (gdf.InsideParts == null)
                                        {
                                            gdf.InsideParts = new SdfPart[numParts];
                                        }

                                        gdf.InsideParts[insidePartsIndex++] = sdfPart;
                                        break;
                                    default:
                                        Debug.LogWarningFormat("Unknown part type '{0}' for part name '{1}'.", partType, partName);
                                        break;
                                }

                                br.Position += 36;
                            }

                            // Skip Lower LOD levels - do we want to use these at all?
                            br.Position += 200;
                        }
                    }
                }

                br.FindNext("ORDF"); // 133 bytes


                br.FindNext("OGEO"); // 104 bytes

                br.ReadInt32();

                gdf.Projectile = new SdfPart
                {
                    Name = br.ReadCString(8),
                    Right = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                    Up = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                    Forward = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                    Position = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                    ParentName = br.ReadCString(8)
                };

                return gdf;
            }
        }
    }
}
