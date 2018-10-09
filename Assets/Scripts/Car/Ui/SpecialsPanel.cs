using System.Collections.Generic;
using Assets.Fileparsers;
using Assets.Scripts.Car.Components;
using Assets.Scripts.System;
using UnityEngine;

namespace Assets.Scripts.Car.UI
{
    public class SpecialsPanel : Panel
    {
        public SpecialComponent[] Items { get; private set; }

        private const int MaxSpecials = 3;
        private readonly bool _initialised;

        public SpecialsPanel(VcfParser.Vcf vcf, Transform firstPersonTransform) : base(firstPersonTransform, "SPC", "zbks_.map")
        {
            int specialsCount = vcf.Specials.Count;
            Items = new SpecialComponent[specialsCount];

            if (ReferenceImage == null)
            {
                return;
            }

            List<SpecialType> specialsList = vcf.Specials;
            for (int i = 0; i < MaxSpecials; ++i)
            {
                int iPlus1 = i + 1;
                string spriteName = "sp_bracket_" + iPlus1;
                I76Sprite housingSprite = SpriteManager.GetSprite("zwpe.map", "housing" + iPlus1);
                ReferenceImage.ApplySprite(spriteName, housingSprite, false);

                I76Sprite offSprite;
                spriteName = "sp_dymo_" + iPlus1;
                if (i < specialsCount)
                {
                    Items[i] = new SpecialComponent(specialsList[i], this, i);
                    Items[i].Type = specialsList[i];
                    I76Sprite onSprite;
                    if (TryGetSpecialSprites(specialsList[i], out onSprite, out offSprite))
                    {
                        Items[i].OnSprite = onSprite;
                        Items[i].OffSprite = offSprite;
                    }
                }
                else
                {
                    offSprite = SpriteManager.GetSprite("zdse.map", "sp_empty_off");
                }

                ReferenceImage.ApplySprite(spriteName, offSprite, false);
            }

            _initialised = true;
            ReferenceImage.UploadToGpu();
        }
        
        public void SetSpecialHealthGroup(int specialIndex, int healthGroup)
        {
            I76Sprite sprite = SpriteManager.GetDiodeSprite(healthGroup);
            if (sprite != null)
            {
                string spriteId = "sp_diode_" + (specialIndex + 1);
                ReferenceImage.ApplySprite(spriteId, sprite, _initialised);
            }
        }
        
        public void SetSpecialEnabledState(int specialIndex, bool specialEnabled)
        {
            string referenceName = "sp_dymo_" + (specialIndex + 1);

            I76Sprite sprite = specialEnabled ? Items[specialIndex].OnSprite : Items[specialIndex].OffSprite;
            ReferenceImage.ApplySprite(referenceName, sprite, _initialised);
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

            if (_initialised)
            {
                ReferenceImage.UploadToGpu();
            }
        }

        private bool TryGetSpecialSprites(SpecialType special, out I76Sprite onSprite, out I76Sprite offSprite)
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
