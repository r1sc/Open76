using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Fileparsers
{
    public class VcfParser
    {
        public class Vcf
        {
            public string VariantName { get; set; }
            public string VdfFilename { get; set; }
            public string VtfFilename { get; set; }
            public uint EngineType { get; set; }
            public uint SuspensionType { get; set; }
            public uint BrakesType { get; set; }
            public string WdfFrontFilename { get; set; }
            public string WdfMidFilename { get; set; }
            public string WdfBackFilename { get; set; }
            public uint ArmorFront { get; set; }
            public uint ArmorLeft { get; set; }
            public uint ArmorRight { get; set; }
            public uint ArmorRear { get; set; }
            public uint ChassisFront { get; set; }
            public uint ChassisLeft { get; set; }
            public uint ChassisRight { get; set; }
            public uint ChassisRear { get; set; }
            public uint ArmorOrChassisLeftToAdd { get; set; }
            public List<VcfWeapon> Weapons { get; set; }
        }

        public class VcfWeapon
        {
            public MountPoint MountPoint { get; set; }
            public string GdfFilename { get; set; }
        }

        public enum MountPoint : uint
        {
            Dropper,
            FirstTop,
            Rear,
            SecondTop
        }

        public static Vcf ReadVcf(string filename)
        {
            using (var br = new Bwd2Reader(filename))
            {
                br.FindNext("VCFC");

                var vcf = new Vcf();
                vcf.VariantName = new string(br.ReadChars(16)).TrimEnd('\0');
                vcf.VdfFilename = new string(br.ReadChars(13)).TrimEnd('\0');
                vcf.VtfFilename = new string(br.ReadChars(13)).TrimEnd('\0');
                vcf.EngineType = br.ReadUInt32();
                vcf.SuspensionType = br.ReadUInt32();
                vcf.BrakesType = br.ReadUInt32();
                vcf.WdfFrontFilename = Encoding.ASCII.GetString(br.ReadBytes(13)).TrimEnd('\0');
                vcf.WdfMidFilename = Encoding.ASCII.GetString(br.ReadBytes(13)).TrimEnd('\0');
                vcf.WdfBackFilename = Encoding.ASCII.GetString(br.ReadBytes(13)).TrimEnd('\0');
                vcf.ArmorFront = br.ReadUInt32();
                vcf.ArmorLeft = br.ReadUInt32();
                vcf.ArmorRight = br.ReadUInt32();
                vcf.ArmorRear = br.ReadUInt32();
                vcf.ChassisFront = br.ReadUInt32();
                vcf.ChassisLeft = br.ReadUInt32();
                vcf.ChassisRight = br.ReadUInt32();
                vcf.ChassisRear = br.ReadUInt32();
                vcf.ArmorOrChassisLeftToAdd = br.ReadUInt32();

                br.FindNext("WEPN");
                vcf.Weapons = new List<VcfWeapon>();
                while (br.Current.Name != "EXIT")
                {
                    var vcfWeapon = new VcfWeapon
                    {
                        MountPoint = (MountPoint)br.ReadUInt32(),
                        GdfFilename = new string(br.ReadChars(13)).TrimEnd('\0')
                    };
                    vcf.Weapons.Add(vcfWeapon);
                    br.Next();
                }

                return vcf;
            }
        }
    }
}
