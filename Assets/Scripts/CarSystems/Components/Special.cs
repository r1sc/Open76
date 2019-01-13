using Assets.Scripts.System;
using Assets.Scripts.System.Fileparsers;

namespace Assets.Scripts.CarSystems.Components
{
    public class Special
    {
        public Special(SpecialType type)
        {
            Type = type;
            Health = 100; // TODO: Parse from somewhere?
            FireRate = 1.0f; // TODO: Parse

            switch (type)
            {
                case SpecialType.RadarJammer:
                    Ammo = 5;
                    break;
                case SpecialType.NitrousOxide:
                    Ammo = 3;
                    break;
                default:
                    Ammo = 1;
                    break;
            }

        }

        public readonly SpecialType Type;
        public int Ammo;
        public int Health;
        public float FireRate;
        public float LastUseTime;
        public I76Sprite OnSprite;
        public I76Sprite OffSprite;
    }
}
