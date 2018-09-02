using Assets.Fileparsers;
using Assets.Scripts.Car.Components;
using Assets.Scripts.System;
using UnityEngine;

namespace Assets.Scripts.Car.UI
{
    public class SpecialsPanel : Panel
    {
        public SpecialsPanel(Transform firstPersonTransform) : base(firstPersonTransform, "SPC", "zbks_.map")
        {
            if (ReferenceImage == null)
            {
                return;
            }
            
            for (int i = 0; i < SpecialsController.MaxSpecials; ++i)
            {
                int iPlus1 = i + 1;
                string spriteName = "sp_bracket_" + iPlus1;
                I76Sprite housingSprite = SpriteManager.GetSprite("zwpe.map", "housing" + iPlus1);
                ReferenceImage.ApplySprite(spriteName, housingSprite, false);
            }
            
            ReferenceImage.UploadToGpu();
        }

        public void SetSpecialHealthGroup(int specialIndex, int healthGroup)
        {
            I76Sprite sprite = SpriteManager.GetDiodeSprite(healthGroup);
            if (sprite != null)
            {
                string spriteId = "sp_diode_" + (specialIndex + 1);
                ReferenceImage.ApplySprite(spriteId, sprite, true);
            }
        }

        public void SetActiveSpecial(int specialIndex, Special[] specials)
        {
            for (int i = 0; i < SpecialsController.MaxSpecials; ++i)
            {
                I76Sprite sprite;
                if (i < specials.Length)
                {
                    sprite = i == specialIndex ? specials[specialIndex].OnSprite : specials[specialIndex].OffSprite;
                }
                else
                {
                    sprite = SpriteManager.GetSprite("zdse.map", "sp_empty_off");
                }

                ReferenceImage.ApplySprite("sp_dymo_" + (i + 1), sprite, false);
            }

            ReferenceImage.UploadToGpu();
        }

        public void SetSpecialAmmoCount(int specialIndex, int ammoCount)
        {
            string numberString = string.Format("{0:000}", ammoCount);
            char digit1 = numberString[0];
            char digit2 = numberString[1];
            char digit3 = numberString[2];

            I76Sprite digitSprite1 = SpriteManager.GetNumberSprite(digit1);
            I76Sprite digitSprite2 = SpriteManager.GetNumberSprite(digit2);
            I76Sprite digitSprite3 = SpriteManager.GetNumberSprite(digit3);

            string spriteIdSuffix = (specialIndex + 1).ToString();
            ReferenceImage.ApplySprite("sp_num_hunds_" + spriteIdSuffix, digitSprite1, false);
            ReferenceImage.ApplySprite("sp_num_tens_" + spriteIdSuffix, digitSprite2, false);
            ReferenceImage.ApplySprite("sp_num_ones_" + spriteIdSuffix, digitSprite3, false);

            ReferenceImage.UploadToGpu();
        }

        public bool TryGetSpecialSprites(SpecialType special, out I76Sprite onSprite, out I76Sprite offSprite)
        {
            string spriteName;
            switch (special)
            {
                case SpecialType.RadarJammer:
                    spriteName = "radar";
                    break;
                case SpecialType.NitrousOxide:
                    spriteName = "nitrous";
                    break;
                case SpecialType.Blower:
                    spriteName = "blower";
                    break;
                case SpecialType.XAustBrake:
                    spriteName = "xaust";
                    break;
                case SpecialType.StructoBumper:
                    spriteName = "structo";
                    break;
                case SpecialType.CurbFeelers:
                    spriteName = "curb";
                    break;
                case SpecialType.MudFlaps:
                    spriteName = "mud";
                    break;
                case SpecialType.HeatedSeats:
                    spriteName = "heated";
                    break;
                case SpecialType.CupHolders:
                    spriteName = "cuphdlr";
                    break;
                default:
                    Debug.LogWarningFormat("GetSpecialSprite was given an unexpected entry - {0}.", special.ToString());
                    onSprite = null;
                    offSprite = null;
                    return false;
            }

            onSprite = SpriteManager.GetSprite("zdse.map", spriteName + "_on");
            offSprite = SpriteManager.GetSprite("zdse.map", spriteName + "_off");

            return onSprite != null && offSprite != null;
        }
    }
}
