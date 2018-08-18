using System.Collections.Generic;
using Assets.Fileparsers;
using Assets.Scripts.System;
using UnityEngine;

namespace Assets.Scripts.Car
{
    public class WeaponsPanel : MonoBehaviour
    {
        private ReferenceImage _referenceImage;
        private SpriteManager _spriteManager;
        private I76Sprite[] _weaponSprites;
        private int _weaponCount;

        public void InitWeapons(VcfParser.Vcf vcf)
        {
            _spriteManager = SpriteManager.Instance;

            _weaponCount = vcf.Weapons.Count;
            _referenceImage = _spriteManager.LoadReferenceImage("zbk_.map");
            if (_referenceImage == null)
            {
                return;
            }

            Transform transformObj = transform;
            bool foundPanel = false;
            foreach (Transform child in transformObj)
            {
                if (child.name.Contains("WEP"))
                {
                    MeshRenderer panelRenderer = child.GetComponent<MeshRenderer>();
                    panelRenderer.material.mainTexture = _referenceImage.MainTexture;
                    foundPanel = true;
                    break;
                }
            }

            if (!foundPanel)
            {
                Debug.LogWarning("Failed to find weapon panel in vehicle's FirstPerson hierarchy.");
            }

            _weaponSprites = new I76Sprite[_weaponCount * 2];

            int spriteIndex = 0;
            List<VcfParser.VcfWeapon> weaponsList = vcf.Weapons;
            for (int i = 0; i < _weaponCount; ++i)
            {
                int weaponIndex = GetWeaponIndex(i);
                string spriteName = "bracket_" + weaponIndex;
                I76Sprite housingSprite = _spriteManager.GetSprite("zwpe.map", "housing" + weaponIndex);
                _referenceImage.ApplySprite(spriteName, housingSprite, false);

                I76Sprite onSprite, offSprite;
                if (TryGetWeaponSprites(weaponsList[i].Gdf, out onSprite, out offSprite))
                {
                    spriteName = "dymo_" + weaponIndex;
                    _weaponSprites[spriteIndex++] = onSprite;
                    _weaponSprites[spriteIndex++] = offSprite;
                    _referenceImage.ApplySprite(spriteName, offSprite, false);
                }

                SetWeaponHealthGroup(weaponIndex, 0, false);
                SetWeaponAmmoCount(weaponIndex, weaponsList[i].Gdf.AmmoCount, false);
            }

            _referenceImage.UploadToGpu();
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
                _referenceImage.ApplySprite(spriteId, sprite, uploadToGpu);
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
            _referenceImage.ApplySprite("num_thous_" + spriteIdSuffix, digitSprite1, false);
            _referenceImage.ApplySprite("num_hunds_" + spriteIdSuffix, digitSprite2, false);
            _referenceImage.ApplySprite("num_tens_" + spriteIdSuffix, digitSprite3, false);
            _referenceImage.ApplySprite("num_ones_" + spriteIdSuffix, digitSprite4, false);

            if (uploadToGpu)
            {
                _referenceImage.UploadToGpu();
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
