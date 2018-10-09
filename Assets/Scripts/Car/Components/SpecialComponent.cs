using Assets.Fileparsers;
using Assets.Scripts.Car.UI;
using Assets.Scripts.System;

namespace Assets.Scripts.Car.Components
{
    public class SpecialComponent : IComponent
    {
        private int _health;
        private bool _enabled;
        private int _ammo;
        private readonly int _index;
        private readonly SpecialsPanel _panel;

        public SpecialComponent(SpecialType type, SpecialsPanel panel, int index)
        {
            Type = type;
            _panel = panel;
            _index = index;

            Health = 0;

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

        public int Health
        {
            get { return _health; }
            set
            {
                _health = value;
                _panel.SetSpecialHealthGroup(_index, 0);
            }
        }

        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                _enabled = value;
                _panel.SetSpecialEnabledState(_index, value);
            }
        }


        public int Ammo
        {
            get { return _ammo; }
            set
            {
                _ammo = value;
                _panel.SetSpecialAmmoCount(_index, value);
            }
        }

        public SpecialType Type { get; set; }
        public I76Sprite OnSprite { get; set; }
        public I76Sprite OffSprite { get; set; }
    }
}
