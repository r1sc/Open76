using System.Collections.Generic;
using Assets.Fileparsers;
using Assets.Scripts.Car.UI;
using Assets.Scripts.System;
using UnityEngine;

namespace Assets.Scripts.Car.Components
{
    public class SpecialsController
    {
        public const int MaxSpecials = 3;

        private int _activeSpecial;
        private readonly Special[] _specials;
        private readonly SpecialsPanel _panel;

        public SpecialsController(VcfParser.Vcf vcf, Transform firstPersonTransform)
        {
            _panel = new SpecialsPanel(firstPersonTransform);

            int specialsCount = vcf.Specials.Count;
            _specials = new Special[specialsCount];

            List<SpecialType> specialsList = vcf.Specials;
            for (int i = 0; i < MaxSpecials; ++i)
            {
                if (i < specialsCount)
                {
                    I76Sprite onSprite, offSprite;
                    if (_panel.TryGetSpecialSprites(specialsList[i], out onSprite, out offSprite))
                    {
                        _specials[i] = new Special(specialsList[i])
                        {
                            OnSprite = onSprite,
                            OffSprite = offSprite
                        };

                        _panel.SetSpecialHealthGroup(i, 0);
                        _panel.SetSpecialAmmoCount(i, _specials[i].Ammo);
                    }
                }
                else
                {
                    _panel.SetSpecialHealthGroup(i, 0);
                }
            }

            _panel.SetActiveSpecial(_activeSpecial, _specials);
        }

        public void CycleSpecial()
        {
            if (_specials.Length < 2)
            {
                return;
            }

            _activeSpecial = ++_activeSpecial % _specials.Length;
            _panel.SetActiveSpecial(_activeSpecial, _specials);
        }

        public void Fire(int specialIndex)
        {
            if (specialIndex == -1)
            {
                specialIndex = _activeSpecial;
            }

            if (_specials.Length <= specialIndex)
            {
                return;
            }

            Special special = _specials[specialIndex];

            if (special.Health <= 0)
            {
                return;
            }

            if (special.Ammo == 0)
            {
                return;
            }

            float currentTime = Time.time;
            if (currentTime - special.LastUseTime < special.FireRate)
            {
                return;
            }

            special.LastUseTime = currentTime;
            _panel.SetSpecialAmmoCount(specialIndex, --special.Ammo);
        }
    }
}
