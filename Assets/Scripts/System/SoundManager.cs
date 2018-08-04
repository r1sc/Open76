using System.Collections.Generic;
using Assets.Fileparsers;
using UnityEngine;

namespace Assets.Scripts.System
{
    public enum SoundEffect
    {
        // Misc / Ambient
        RadarGrowl,
        RadarBeep,
        OpenMap,
        Wind,
        Fireworks,
        Helicopter,
        PoliceSiren,
        Nitro,
        WeaponSwitch,
        WeaponNoAmmo,
        RadarJammer,
        LockOnWarning,
        ObjectiveChanged,
        RadioMisc,
        Roger,
        WeaponCritical,
        WeaponMissing,

        // Drop weapons
        DropBlox,
        DropCaltrops,
        DropErase,
        DropLandmine,
        DropOil,
        DropFire,

        // Engines
        Engine1Loop,
        Engine2Loop,
        Engine3Loop,
        Engine4Loop,
        TruckEngineStart,
        TruckEngineLoop,
        TankEngineStart,
        TankEngineLoop,
        Engine1Start1,
        Engine1Start2,
        Engine1Start3,
        Engine2Start1,
        Engine2Start2,
        Engine2Start3,
        Engine3Start1,
        Engine3Start2,
        Engine3Start3,
        EngineStall,

        // Vehicle Misc / Surfaces
        FireDamage,
        HandgunFire,
        HandgunImpact1,
        HandgunImpact2,
        MissileLock1,
        MissileLock2,
        MissileLock3,
        TireBlow,
        TireCaltrops,
        TireFlatLoop,
        TireSkid1,
        TireSkid2,
        TireSkid3,
        TireSkid4,
        VehicleBalloon,
        VehicleDirt,
        VehicleGravel,
        VehicleOil,
        VehicleSand,
        VehicleExplode,
        Horn1,
        Horn2,
        Horn3,
        Horn4,
        Horn5,
        Horn6,
        VehicleLanding1,
        VehicleLanding2,
        VehicleSignImpact,
        VehicleHardImpact1,
        VehicleHardImpact2,
        VehicleHardImpact3,
        VehicleShiftGear,
        VehicleSkid,
        VehicleCollision,
        VehicleHit1,
        VehicleHit2,
        VehicleHit3,
        VehicleHit4,
        VehicleHit5,
        VehicleUfo,

        // Weapons
        Weapon30Cal,
        Weapon50Cal,
        Weapon762mm,
        Weapon20mm,
        Weapon25mm,
        Weapon30mm,
        WeaponHades,
        WeaponFireRite,
        WeaponAIM,
        WeaponDrRadar,
        WeaponCherub,
        WeaponFlamethrower,
        WeaponGasLauncher,
        WeaponNapalmHose,
        WeaponPyroTomic,
        WeaponClusterBomb,
        WeaponTank,
        WeaponHowitzer,
        WeaponEZKill,
        WeaponHEMortar,
        WeaponWPMortar,
        WeaponMortar1,
        WeaponMortar2,

        // Explosions
        ExplosionBridge,
        ExplosionBuilding,
        ExplosionCar,
        Explosion1,
        Explosion2,
        Explosion3,
        ExplosionFire,
        ExplosionGas,
        ExplosionMediumVehicle1,
        ExplosionMediumVehicle2,
        ExlosionSmallVehicle1,
        ExlosionSmallVehicle2,
        BulletRicochet1,
        BulletRicochet2
    }

    public class SoundManager
    {
        private readonly Dictionary<SoundEffect, Gpw> _soundEffectLookup;

        private static SoundManager _instance;
        
        public static SoundManager Instance
        {
            get
            {
                return _instance ?? (_instance = new SoundManager());
            }
        }

        private SoundManager()
        {
            _soundEffectLookup = new Dictionary<SoundEffect, Gpw>();
        }

        public void PreloadSounds()
        {
            if (_soundEffectLookup.Count > 0)
            {
                return;
            }

            // Common / Ambient
            _soundEffectLookup.Add(SoundEffect.RadarGrowl, LoadSoundFile("cgrowl.gpw"));
            _soundEffectLookup.Add(SoundEffect.RadarBeep, LoadSoundFile("cradar.gpw"));
            _soundEffectLookup.Add(SoundEffect.OpenMap, LoadSoundFile("cmap2.gpw"));
            _soundEffectLookup.Add(SoundEffect.Wind, LoadSoundFile("adwind.gpw"));
            _soundEffectLookup.Add(SoundEffect.Fireworks, LoadSoundFile("afworks.gpw"));
            _soundEffectLookup.Add(SoundEffect.Helicopter, LoadSoundFile("aheli.gpw"));
            _soundEffectLookup.Add(SoundEffect.PoliceSiren, LoadSoundFile("aps2.gpw"));
            _soundEffectLookup.Add(SoundEffect.Nitro, LoadSoundFile("bnitro.gpw"));
            _soundEffectLookup.Add(SoundEffect.WeaponSwitch, LoadSoundFile("cammo.gpw"));
            _soundEffectLookup.Add(SoundEffect.WeaponNoAmmo, LoadSoundFile("wclick.gpw"));
            _soundEffectLookup.Add(SoundEffect.RadarJammer, LoadSoundFile("cjamm.gpw"));
            _soundEffectLookup.Add(SoundEffect.LockOnWarning, LoadSoundFile("clockon.gpw"));
            _soundEffectLookup.Add(SoundEffect.ObjectiveChanged, LoadSoundFile("cnote.gpw"));
            _soundEffectLookup.Add(SoundEffect.RadioMisc, LoadSoundFile("cmike.gpw"));
            _soundEffectLookup.Add(SoundEffect.Roger, LoadSoundFile("croger.gpw"));
            _soundEffectLookup.Add(SoundEffect.WeaponCritical, LoadSoundFile("cwstat.gpw"));
            _soundEffectLookup.Add(SoundEffect.WeaponMissing, LoadSoundFile("wmiss.gpw"));

            // Drop weapons
            _soundEffectLookup.Add(SoundEffect.DropBlox, LoadSoundFile("dblox.gpw"));
            _soundEffectLookup.Add(SoundEffect.DropCaltrops, LoadSoundFile("dcaltrop.gpw"));
            _soundEffectLookup.Add(SoundEffect.DropErase, LoadSoundFile("derase.gpw"));
            _soundEffectLookup.Add(SoundEffect.DropFire, LoadSoundFile("dfire.gpw"));
            _soundEffectLookup.Add(SoundEffect.DropLandmine, LoadSoundFile("dmines1.gpw"));
            _soundEffectLookup.Add(SoundEffect.DropOil, LoadSoundFile("doil.gpw"));

            // Engine
            _soundEffectLookup.Add(SoundEffect.Engine1Loop, LoadSoundFile("eihp.gpw"));
            _soundEffectLookup.Add(SoundEffect.Engine2Loop, LoadSoundFile("einp1.gpw"));
            _soundEffectLookup.Add(SoundEffect.Engine3Loop, LoadSoundFile("eishp.gpw"));
            _soundEffectLookup.Add(SoundEffect.Engine4Loop, LoadSoundFile("eisv.gpw"));
            _soundEffectLookup.Add(SoundEffect.TruckEngineStart, LoadSoundFile("esmarx.gpw"));
            _soundEffectLookup.Add(SoundEffect.TruckEngineLoop, LoadSoundFile("eimarx.gpw"));
            _soundEffectLookup.Add(SoundEffect.TankEngineStart, LoadSoundFile("estank.gpw"));
            _soundEffectLookup.Add(SoundEffect.TankEngineLoop, LoadSoundFile("eitank.gpw"));
            _soundEffectLookup.Add(SoundEffect.Engine1Start1, LoadSoundFile("eshp1.gpw"));
            _soundEffectLookup.Add(SoundEffect.Engine1Start2, LoadSoundFile("eshp2.gpw"));
            _soundEffectLookup.Add(SoundEffect.Engine1Start3, LoadSoundFile("eshp3.gpw"));
            _soundEffectLookup.Add(SoundEffect.Engine2Start1, LoadSoundFile("esnp1.gpw"));
            _soundEffectLookup.Add(SoundEffect.Engine2Start2, LoadSoundFile("esnp2.gpw"));
            _soundEffectLookup.Add(SoundEffect.Engine2Start3, LoadSoundFile("esnp3.gpw"));
            _soundEffectLookup.Add(SoundEffect.Engine3Start1, LoadSoundFile("esshp1.gpw"));
            _soundEffectLookup.Add(SoundEffect.Engine3Start2, LoadSoundFile("esshp2.gpw"));
            _soundEffectLookup.Add(SoundEffect.Engine3Start3, LoadSoundFile("esshp3.gpw"));
            _soundEffectLookup.Add(SoundEffect.EngineStall, LoadSoundFile("essv.gpw"));

            // Vehicle Misc / Surfaces
            _soundEffectLookup.Add(SoundEffect.FireDamage, LoadSoundFile("fdmg1.gpw"));
            _soundEffectLookup.Add(SoundEffect.HandgunFire, LoadSoundFile("h45ch.gpw"));
            _soundEffectLookup.Add(SoundEffect.HandgunImpact1, LoadSoundFile("hssr1.gpw"));
            _soundEffectLookup.Add(SoundEffect.HandgunImpact2, LoadSoundFile("hssr2.gpw"));
            _soundEffectLookup.Add(SoundEffect.MissileLock1, LoadSoundFile("msllock1.gpw"));
            _soundEffectLookup.Add(SoundEffect.MissileLock2, LoadSoundFile("msllock2.gpw"));
            _soundEffectLookup.Add(SoundEffect.MissileLock3, LoadSoundFile("msllock3.gpw"));
            _soundEffectLookup.Add(SoundEffect.TireBlow, LoadSoundFile("tblow.gpw"));
            _soundEffectLookup.Add(SoundEffect.TireCaltrops, LoadSoundFile("tcaltrop.gpw"));
            _soundEffectLookup.Add(SoundEffect.TireFlatLoop, LoadSoundFile("tflat.gpw"));
            _soundEffectLookup.Add(SoundEffect.TireSkid1, LoadSoundFile("tskid1.gpw"));
            _soundEffectLookup.Add(SoundEffect.TireSkid2, LoadSoundFile("tskid2.gpw"));
            _soundEffectLookup.Add(SoundEffect.TireSkid3, LoadSoundFile("tskid3.gpw"));
            _soundEffectLookup.Add(SoundEffect.TireSkid4, LoadSoundFile("tskid4.gpw"));
            _soundEffectLookup.Add(SoundEffect.VehicleBalloon, LoadSoundFile("vballoon.gpw"));
            _soundEffectLookup.Add(SoundEffect.VehicleDirt, LoadSoundFile("vcddirt.gpw"));
            _soundEffectLookup.Add(SoundEffect.VehicleGravel, LoadSoundFile("vcdgrav.gpw"));
            _soundEffectLookup.Add(SoundEffect.VehicleOil, LoadSoundFile("vcdoil.gpw"));
            _soundEffectLookup.Add(SoundEffect.VehicleSand, LoadSoundFile("vcdsand.gpw"));
            _soundEffectLookup.Add(SoundEffect.VehicleExplode, LoadSoundFile("vexplode.gpw"));
            _soundEffectLookup.Add(SoundEffect.Horn1, LoadSoundFile("vhorn1.gpw"));
            _soundEffectLookup.Add(SoundEffect.Horn2, LoadSoundFile("vhorn2.gpw"));
            _soundEffectLookup.Add(SoundEffect.Horn3, LoadSoundFile("vhorn3.gpw"));
            _soundEffectLookup.Add(SoundEffect.Horn4, LoadSoundFile("vhorn4.gpw"));
            _soundEffectLookup.Add(SoundEffect.Horn5, LoadSoundFile("vhorn5.gpw"));
            _soundEffectLookup.Add(SoundEffect.Horn6, LoadSoundFile("vhorn6.gpw"));
            _soundEffectLookup.Add(SoundEffect.VehicleLanding1, LoadSoundFile("vland.gpw"));
            _soundEffectLookup.Add(SoundEffect.VehicleLanding2, LoadSoundFile("vlanding.gpw"));
            _soundEffectLookup.Add(SoundEffect.VehicleSignImpact, LoadSoundFile("vnvco3.gpw"));
            _soundEffectLookup.Add(SoundEffect.VehicleHardImpact1, LoadSoundFile("vnvcs1.gpw"));
            _soundEffectLookup.Add(SoundEffect.VehicleHardImpact2, LoadSoundFile("vnvcs3.gpw"));
            _soundEffectLookup.Add(SoundEffect.VehicleHardImpact3, LoadSoundFile("vnvcs5.gpw"));
            _soundEffectLookup.Add(SoundEffect.VehicleShiftGear, LoadSoundFile("vshif1a.gpw"));
            _soundEffectLookup.Add(SoundEffect.VehicleSkid, LoadSoundFile("vskid.gpw"));
            _soundEffectLookup.Add(SoundEffect.VehicleCollision, LoadSoundFile("vtcoll.gpw"));
            _soundEffectLookup.Add(SoundEffect.VehicleHit1, LoadSoundFile("vvbo1.gpw"));
            _soundEffectLookup.Add(SoundEffect.VehicleHit2, LoadSoundFile("vvcbb3.gpw"));
            _soundEffectLookup.Add(SoundEffect.VehicleHit3, LoadSoundFile("vvch2.gpw"));
            _soundEffectLookup.Add(SoundEffect.VehicleHit4, LoadSoundFile("vvcre2.gpw"));
            _soundEffectLookup.Add(SoundEffect.VehicleHit5, LoadSoundFile("vvcre3.gpw"));
            _soundEffectLookup.Add(SoundEffect.VehicleUfo, LoadSoundFile("vufo.gpw"));

            // Weapons
            _soundEffectLookup.Add(SoundEffect.WeaponPyroTomic, LoadSoundFile("wbalflam.gpw"));
            _soundEffectLookup.Add(SoundEffect.WeaponClusterBomb, LoadSoundFile("wcbl.gpw"));
            _soundEffectLookup.Add(SoundEffect.WeaponCherub, LoadSoundFile("wcherub.gpw"));
            _soundEffectLookup.Add(SoundEffect.WeaponNapalmHose, LoadSoundFile("whflame.gpw"));
            _soundEffectLookup.Add(SoundEffect.Weapon762mm, LoadSoundFile("wlmgun.gpw"));
            _soundEffectLookup.Add(SoundEffect.Weapon30Cal, LoadSoundFile("wmmgun.gpw"));
            _soundEffectLookup.Add(SoundEffect.Weapon50Cal, LoadSoundFile("whmgun.gpw"));
            _soundEffectLookup.Add(SoundEffect.WeaponHowitzer, LoadSoundFile("whowitz.gpw"));
            _soundEffectLookup.Add(SoundEffect.WeaponDrRadar, LoadSoundFile("whsm.gpw"));
            _soundEffectLookup.Add(SoundEffect.WeaponFlamethrower, LoadSoundFile("wlflame.gpw"));
            _soundEffectLookup.Add(SoundEffect.Weapon20mm, LoadSoundFile("wlcan.gpw"));
            _soundEffectLookup.Add(SoundEffect.Weapon25mm, LoadSoundFile("wmcan.gpw"));
            _soundEffectLookup.Add(SoundEffect.Weapon30mm, LoadSoundFile("whcan.gpw"));
            _soundEffectLookup.Add(SoundEffect.WeaponHades, LoadSoundFile("whades.gpw"));
            _soundEffectLookup.Add(SoundEffect.WeaponGasLauncher, LoadSoundFile("wmflame.gpw"));
            _soundEffectLookup.Add(SoundEffect.BulletRicochet1, LoadSoundFile("wmgr1.gpw"));
            _soundEffectLookup.Add(SoundEffect.BulletRicochet2, LoadSoundFile("wmgr2.gpw"));
            _soundEffectLookup.Add(SoundEffect.WeaponAIM, LoadSoundFile("wrhm.gpw"));
            _soundEffectLookup.Add(SoundEffect.WeaponFireRite, LoadSoundFile("wrock.gpw"));
            _soundEffectLookup.Add(SoundEffect.WeaponTank, LoadSoundFile("wtank.gpw"));
            _soundEffectLookup.Add(SoundEffect.WeaponEZKill, LoadSoundFile("wezk.gpw"));
            _soundEffectLookup.Add(SoundEffect.WeaponHEMortar, LoadSoundFile("wgl.gpw"));
            _soundEffectLookup.Add(SoundEffect.WeaponWPMortar, LoadSoundFile("wwpgl.gpw"));
            _soundEffectLookup.Add(SoundEffect.WeaponMortar1, LoadSoundFile("wchem.gpw"));
            _soundEffectLookup.Add(SoundEffect.WeaponMortar2, LoadSoundFile("wcvr.gpw"));

            // Explosions
            _soundEffectLookup.Add(SoundEffect.ExplosionBridge, LoadSoundFile("xbridge.gpw"));
            _soundEffectLookup.Add(SoundEffect.ExplosionBuilding, LoadSoundFile("xbuild.gpw"));
            _soundEffectLookup.Add(SoundEffect.ExplosionCar, LoadSoundFile("xcar.gpw"));
            _soundEffectLookup.Add(SoundEffect.Explosion1, LoadSoundFile("xemt1.gpw"));
            _soundEffectLookup.Add(SoundEffect.Explosion2, LoadSoundFile("xemt2.gpw"));
            _soundEffectLookup.Add(SoundEffect.Explosion3, LoadSoundFile("xms2.gpw"));
            _soundEffectLookup.Add(SoundEffect.ExplosionFire, LoadSoundFile("xfire1.gpw"));
            _soundEffectLookup.Add(SoundEffect.ExplosionGas, LoadSoundFile("xgas.gpw"));
            _soundEffectLookup.Add(SoundEffect.ExplosionMediumVehicle1, LoadSoundFile("xmv1.gpw"));
            _soundEffectLookup.Add(SoundEffect.ExplosionMediumVehicle2, LoadSoundFile("xmv2.gpw"));
            _soundEffectLookup.Add(SoundEffect.ExlosionSmallVehicle1, LoadSoundFile("xsv1.gpw"));
            _soundEffectLookup.Add(SoundEffect.ExlosionSmallVehicle2, LoadSoundFile("xsv2.gpw"));
        }

        public AudioClip GetSoundClip(SoundEffect soundEffect)
        {
            return _soundEffectLookup[soundEffect].Clip;
        }

        private Gpw LoadSoundFile(string fileName)
        {
            return GpwParser.ParseGpw(fileName);
        }
    }
}
