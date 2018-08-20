using System.Collections.Generic;
using Assets.Fileparsers;
using Assets.Scripts.Car.Components;
using Assets.Scripts.System;
using UnityEngine;

namespace Assets.Scripts.Car.UI
{
    public class WeaponsPanel : Panel
    {
        public WeaponComponent[] Items { get; private set; }

        private readonly bool _initialised;

        public WeaponsPanel(VcfParser.Vcf vcf, Transform firstPersonTransform) : base(firstPersonTransform, "WEP", "zbk_.map")
        {
            int weaponCount = vcf.Weapons.Count;
            Items = new WeaponComponent[weaponCount];

            if (ReferenceImage == null)
            {
                return;
            }
            
            List<VcfParser.VcfWeapon> weaponsList = vcf.Weapons;
            for (int i = 0; i < weaponCount; ++i)
            {
                int iPlus1 = i + 1;
                string spriteName = "bracket_" + iPlus1;
                I76Sprite housingSprite = SpriteManager.GetSprite("zwpe.map", "housing" + iPlus1);
                ReferenceImage.ApplySprite(spriteName, housingSprite, false);

                I76Sprite onSprite, offSprite;
                if (TryGetWeaponSprites(weaponsList[i].Gdf, out onSprite, out offSprite))
                {
                    spriteName = "dymo_" + iPlus1;
                    Items[i] = new WeaponComponent(weaponsList[i].Gdf.AmmoCount, this, i);
                    Items[i].OnSprite = onSprite;
                    Items[i].OffSprite = offSprite;
                    ReferenceImage.ApplySprite(spriteName, offSprite, false);
                }
            }

            _initialised = true;
            ReferenceImage.UploadToGpu();
        }
        
        public void SetWeaponHealthGroup(int weaponIndex, int healthGroup)
        {
            I76Sprite sprite = SpriteManager.GetDiodeSprite(healthGroup);
            if (sprite != null)
            {
                string spriteId = "diode_" + (weaponIndex + 1);
                ReferenceImage.ApplySprite(spriteId, sprite, _initialised);
            }
        }
        
        public void SetWeaponEnabledState(int weaponIndex, bool weaponEnabled)
        {
            string referenceName = "dymo_" + (weaponIndex + 1);

            I76Sprite sprite = weaponEnabled ? Items[weaponIndex].OnSprite : Items[weaponIndex].OffSprite;
            ReferenceImage.ApplySprite(referenceName, sprite, _initialised);
        }
        
        public void SetWeaponAmmoCount(int weaponIndex, int ammoCount)
        {
            string numberString = string.Format("{0:0000}", ammoCount);
            char digit1 = numberString[0];
            char digit2 = numberString[1];
            char digit3 = numberString[2];
            char digit4 = numberString[3];

            I76Sprite digitSprite1 = SpriteManager.GetNumberSprite(digit1);
            I76Sprite digitSprite2 = SpriteManager.GetNumberSprite(digit2);
            I76Sprite digitSprite3 = SpriteManager.GetNumberSprite(digit3);
            I76Sprite digitSprite4 = SpriteManager.GetNumberSprite(digit4);

            string spriteIdSuffix = (weaponIndex + 1).ToString();
            ReferenceImage.ApplySprite("num_thous_" + spriteIdSuffix, digitSprite1, false);
            ReferenceImage.ApplySprite("num_hunds_" + spriteIdSuffix, digitSprite2, false);
            ReferenceImage.ApplySprite("num_tens_" + spriteIdSuffix, digitSprite3, false);
            ReferenceImage.ApplySprite("num_ones_" + spriteIdSuffix, digitSprite4, false);

            if (_initialised)
            {
                ReferenceImage.UploadToGpu();
            }
        }

        private bool TryGetWeaponSprites(Gdf weaponGdf, out I76Sprite onSprite, out I76Sprite offSprite)
        {
            string spriteName;

            switch (weaponGdf.Name)
            {
                case "30cal MG":
                    spriteName = "30cal_mg";
                    break;
                case "Oil Slick":
                    spriteName = "oilslick";
                    break;
                case "FireRite Rkt":
                    spriteName = "fr_rocket";
                    break;
                case "Landmines":
                    spriteName = "landmines";
                    break;
                case "Fire-Dropper":
                    spriteName = "firedropper";
                    break;
                case "7.62mm MG":
                    spriteName = "762_mg";
                    break;
                default:
                    Debug.LogWarningFormat("GetWeaponSprite for weapon '{0}' not implemented.", weaponGdf.Name);
                    onSprite = null;
                    offSprite = null;
                    return false;
            }

            onSprite = SpriteManager.GetSprite("zdue.map", spriteName + "_on");
            offSprite = SpriteManager.GetSprite("zdue.map", spriteName + "_off");

            return onSprite != null && offSprite != null;
        }
    }
}
