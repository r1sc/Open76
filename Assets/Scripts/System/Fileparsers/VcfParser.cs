using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.System.Fileparsers
{
    public enum SpecialType
    {
        None,
        RadarJammer,
        NitrousOxide,
        Blower,
        XAustBrake,
        StructoBumper,
        CurbFeelers,
        MudFlaps,
        HeatedSeats,
        CupHolders
    }


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
        public List<SpecialType> Specials { get; set; }
        public Wdf FrontWheelDef { get; set; }
        public Wdf MidWheelDef { get; set; }
        public Wdf BackWheelDef { get; set; }
    }

    public class VcfWeapon
    {
        public int MountPoint { get; set; }
        public string GdfFilename { get; set; }
        public Gdf Gdf { get; set; }
        public bool RearFacing { get; set; }
        public Transform Transform { get; set; }
    }

    public class VcfParser
    {
        public static Vcf ParseVcf(string filename)
        {
            Vcf vcf = new Vcf();
            using (Bwd2Reader br = new Bwd2Reader(filename))
            {
                br.FindNext("VCFC");

                vcf.VariantName = br.ReadCString(16);
                vcf.VdfFilename = br.ReadCString(13);
                vcf.VtfFilename = br.ReadCString(13);
                vcf.EngineType = br.ReadUInt32();
                vcf.SuspensionType = br.ReadUInt32();
                vcf.BrakesType = br.ReadUInt32();
                vcf.WdfFrontFilename = br.ReadCString(13);
                vcf.WdfMidFilename = br.ReadCString(13);
                vcf.WdfBackFilename = br.ReadCString(13);
                vcf.ArmorFront = br.ReadUInt32();
                vcf.ArmorLeft = br.ReadUInt32();
                vcf.ArmorRight = br.ReadUInt32();
                vcf.ArmorRear = br.ReadUInt32();
                vcf.ChassisFront = br.ReadUInt32();
                vcf.ChassisLeft = br.ReadUInt32();
                vcf.ChassisRight = br.ReadUInt32();
                vcf.ChassisRear = br.ReadUInt32();
                vcf.ArmorOrChassisLeftToAdd = br.ReadUInt32();

                vcf.Specials = new List<SpecialType>();
                if (br.TryFindNext("SPEC"))
                {
                    vcf.Specials.Add((SpecialType)br.ReadInt32());
                }
                if (br.TryFindNext("SPEC"))
                {
                    vcf.Specials.Add((SpecialType)br.ReadInt32());
                }
                if (br.TryFindNext("SPEC"))
                {
                    vcf.Specials.Add((SpecialType)br.ReadInt32());
                }

                br.FindNext("WEPN");
                vcf.Weapons = new List<VcfWeapon>();
                while (br.Current != null && br.Current.Name != "EXIT")
                {
                    VcfWeapon vcfWeapon = new VcfWeapon
                    {
                        MountPoint = br.ReadInt32(),
                        GdfFilename = br.ReadCString(13)
                    };

                    vcfWeapon.Gdf = GdfParser.ParseGdf(vcfWeapon.GdfFilename);

                    vcf.Weapons.Add(vcfWeapon);
                    br.Next();
                }
            }

            if (vcf.WdfFrontFilename.ToUpper() != "NULL")
                vcf.FrontWheelDef = WdfParser.ParseWdf(vcf.WdfFrontFilename);
            if (vcf.WdfMidFilename.ToUpper() != "NULL")
                vcf.MidWheelDef = WdfParser.ParseWdf(vcf.WdfMidFilename);
            if (vcf.WdfBackFilename.ToUpper() != "NULL")
                vcf.BackWheelDef = WdfParser.ParseWdf(vcf.WdfBackFilename);

            return vcf;
        }
    }
}
