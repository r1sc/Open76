using Assets.Scripts.Car.UI;
using Assets.Scripts.System;

namespace Assets.Scripts.Car.Components
{
    public class WeaponComponent : IComponent
    {
        private int _health;
        private bool _enabled;
        private int _ammo;
        private readonly int _index;
        private readonly WeaponsPanel _panel;

        public WeaponComponent(int ammo, WeaponsPanel panel, int index)
        {
            _panel = panel;
            _index = index;
            Health = 0;
            Ammo = ammo;
        }

        public int Health
        {
            get { return _health; }
            set
            {
                _health = value;
                _panel.SetWeaponHealthGroup(_index, 0);
            }
        }

        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                _enabled = value;
                _panel.SetWeaponEnabledState(_index, value);
            }
        }
        
        public int Ammo
        {
            get { return _ammo; }
            set
            {
                _ammo = value;
                _panel.SetWeaponAmmoCount(_index, value);
            }
        }

        public I76Sprite OnSprite { get; set; }
        public I76Sprite OffSprite { get; set; }
    }
}
