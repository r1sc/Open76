using System.Collections.Generic;
using Assets.Fileparsers;
using Assets.Scripts.System;
using UnityEngine;

namespace Assets.Scripts.Car
{
    public class WeaponsPanel : MonoBehaviour
    {
        public ReferenceImage ReferenceImage;

        public Texture2D Texture;

        private SpriteManager _spriteManager;
        private I76Sprite[] _weaponSprites;
        private int _weaponCount;

        public void InitWeapons(VcfParser.Vcf vcf)
        {
            _spriteManager = SpriteManager.Instance;

            _weaponCount = vcf.Weapons.Count;
            ReferenceImage = _spriteManager.LoadReferenceImage("zbk_.map");
            if (ReferenceImage == null)
            {
                return;
            }

            Texture = ReferenceImage.MainTexture;

            _weaponSprites = new I76Sprite[_weaponCount * 2];

            int spriteIndex = 0;
            List<VcfParser.VcfWeapon> weaponsList = vcf.Weapons;
            for (int i = 0; i < _weaponCount; ++i)
            {
                int weaponIndex = GetWeaponIndex(i);
                string spriteName = "bracket_" + weaponIndex;
                I76Sprite housingSprite = _spriteManager.GetSprite("zwpe.map", "housing" + weaponIndex);
                ReferenceImage.ApplySprite(spriteName, housingSprite, false);

                I76Sprite onSprite, offSprite;
                if (TryGetWeaponSprites(weaponsList[i].Gdf, out onSprite, out offSprite))
                {
                    spriteName = "dymo_" + weaponIndex;
                    _weaponSprites[spriteIndex++] = onSprite;
                    _weaponSprites[spriteIndex++] = offSprite;
                    ReferenceImage.ApplySprite(spriteName, offSprite, false);
                }

                SetWeaponHealthGroup(weaponIndex, 0, false);
                SetWeaponAmmoCount(weaponIndex, weaponsList[i].Gdf.AmmoCount, false);
            }

            ReferenceImage.UploadToGpu();
        }

        private int GetWeaponIndex(int index)
        {
            return _weaponCount - index;
        }

        public void SetWeaponHealthGroup(int weaponIndex, int healthGroup, bool uploadToGpu)
        {
            I76Sprite sprite = _spriteManager.GetDiodeSprite(healthGroup);
            if (sprite != null)
            {
                string spriteId = "diode_" + weaponIndex;
                ReferenceImage.ApplySprite(spriteId, sprite, uploadToGpu);
            }
        }

        public void SetWeaponAmmoCount(int weaponIndex, int ammoCount, bool uploadToGpu)
        {
            string numberString = string.Format("{0:0000}", ammoCount);
            char digit1 = numberString[0];
            char digit2 = numberString[1];
            char digit3 = numberString[2];
            char digit4 = numberString[3];

            I76Sprite digitSprite1 = _spriteManager.GetNumberSprite(digit1);
            I76Sprite digitSprite2 = _spriteManager.GetNumberSprite(digit2);
            I76Sprite digitSprite3 = _spriteManager.GetNumberSprite(digit3);
            I76Sprite digitSprite4 = _spriteManager.GetNumberSprite(digit4);

            string spriteIdSuffix = weaponIndex.ToString();
            ReferenceImage.ApplySprite("num_thous_" + spriteIdSuffix, digitSprite1, false);
            ReferenceImage.ApplySprite("num_hunds_" + spriteIdSuffix, digitSprite2, false);
            ReferenceImage.ApplySprite("num_tens_" + spriteIdSuffix, digitSprite3, false);
            ReferenceImage.ApplySprite("num_ones_" + spriteIdSuffix, digitSprite4, false);

            if (uploadToGpu)
            {
                ReferenceImage.UploadToGpu();
            }
        }

        private bool TryGetWeaponSprites(Gdf weaponGdf, out I76Sprite onSprite, out I76Sprite offSprite)
        {
            string onSpriteName;
            string offSpriteName;

            onSprite = null;
            offSprite = null;

            switch (weaponGdf.Name)
            {
                case "30cal MG":
                    onSpriteName = "30cal_mg_on";
                    offSpriteName = "30cal_mg_off";
                    break;
                case "Oil Slick":
                    onSpriteName = "oilslick_on";
                    offSpriteName = "oilslick_off";
                    break;
                case "FireRite Rkt":
                    onSpriteName = "fr_rocket_on";
                    offSpriteName = "fr_rocket_off";
                    break;
                case "Landmines":
                    onSpriteName = "landmines_on";
                    offSpriteName = "landmines_off";
                    break;
                case "Fire-Dropper":
                    onSpriteName = "firedropper_on";
                    offSpriteName = "firedropper_off";
                    break;
                case "7.62mm MG":
                    onSpriteName = "762_mg_on";
                    offSpriteName = "762_mg_off";
                    break;
                default:
                    Debug.LogWarningFormat("GetWeaponSprite for weapon '{0}' not implemented.", weaponGdf.Name);
                    return false;
            }

            onSprite = _spriteManager.GetSprite("zdue.map", onSpriteName);
            offSprite = _spriteManager.GetSprite("zdue.map", offSpriteName);

            if (onSprite != null && offSprite != null)
            {
                return true;
            }

            return false;
        }
    }
}
