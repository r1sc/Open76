﻿using Assets.Scripts.CarSystems.Components;
using Assets.Scripts.System;
using Assets.Scripts.System.Fileparsers;
using UnityEngine;

namespace Assets.Scripts.CarSystems.Ui
{
    public class WeaponsPanel : Panel
    {
        private readonly I76Sprite _separatorSprite;

        public int SeparatorIndex { get; set; }

        public WeaponsPanel(Transform firstPersonTransform) : base(firstPersonTransform, "WEP", "zbk_.map")
        {
            if (ReferenceImage == null)
            {
                return;
            }
            
            _separatorSprite = SpriteManager.GetSprite("zbar_.map", "sepbar");
        }
        
        public void UpdateActiveWeaponGroup(int weaponGroup, Weapon[] weapons)
        {
            for (int i = 0; i < weapons.Length; ++i)
            {
                string referenceName = "dymo_" + (i + 1);
                
                bool sameGroup = weapons[i].WeaponGroupOffset == weaponGroup;
                I76Sprite sprite = sameGroup ? weapons[i].OnSprite : weapons[i].OffSprite;
                ReferenceImage.ApplySprite(referenceName, sprite, false);

                if (SeparatorIndex == i)
                {
                    ReferenceImage.ApplySprite("separator_" + (i + 1), _separatorSprite, false);
                }
            }

            ReferenceImage.UploadToGpu();
        }

        public void SetWeaponCount(int weaponCount)
        {
            for (int i = 0; i < weaponCount; ++i)
            {
                int iPlus1 = i + 1;
                I76Sprite sprite = SpriteManager.GetSprite("zwpe.map", "housing" + iPlus1);
                ReferenceImage.ApplySprite("bracket_" + iPlus1, sprite, false);
            }

            ReferenceImage.UploadToGpu();
        }

        public void SetWeaponHealthGroup(int weaponIndex, int healthGroup)
        {
            I76Sprite sprite = SpriteManager.GetDiodeSprite(healthGroup);
            if (sprite != null)
            {
                string spriteId = "diode_" + (weaponIndex + 1);
                ReferenceImage.ApplySprite(spriteId, sprite, true);
            }
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

            ReferenceImage.UploadToGpu();
        }

        public bool TryGetWeaponSprites(Gdf weaponGdf, out I76Sprite onSprite, out I76Sprite offSprite)
        {
            string spriteName;

            switch (weaponGdf.Name)
            {
                case "30cal MG":
                    spriteName = "30cal_mg";
                    break;
                case "50cal MG":
                    spriteName = "50cal_mg";
                    break;
                case "20mm Cannnon":
                    spriteName = "20mm_can";
                    break;
                case "25mm Cannon":
                    spriteName = "25mm_can";
                    break;
                case "30mm Cannon":
                    spriteName = "30mm_can";
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
                case "Gas Launcher":
                    spriteName = "gaslaunchr";
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
