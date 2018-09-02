using System;
using Assets.Scripts.System;
using UnityEngine;

namespace Assets.Scripts.Car.UI
{
    public class GearPanel : Panel
    {
        private readonly I76Sprite _backgroundSprite;
        private readonly I76Sprite _arrowSprite;
        private readonly Color[] _cleanBackgroundPixels;
        private char _activeGear;

        public GearPanel(Transform firstPersonTransform) : base(firstPersonTransform, "GER", "zgear101.map")
        {
            if (ReferenceImage == null)
            {
                return;
            }

            _arrowSprite = SpriteManager.GetSprite("zgeare.map", "arrow");
            _backgroundSprite = SpriteManager.GetSprite("zgear2e.map", "prndback");
            _cleanBackgroundPixels = _backgroundSprite.Pixels;

            ActiveGear = 'D';
        }

        public char ActiveGear
        {
            get { return _activeGear; }
            set
            {
                switch (value)
                {
                    case 'P':
                        ApplyGearSprite("park");
                        break;
                    case 'R':
                        ApplyGearSprite("reverse");
                        break;
                    case 'N':
                        ApplyGearSprite("neutral");
                        break;
                    case 'D':
                        ApplyGearSprite("drive");
                        break;
                    case '1':
                        ApplyGearSprite("first");
                        break;
                    case '2':
                        ApplyGearSprite("second");
                        break;
                    default:
                        Debug.LogError("Invalid gear specified.");
                        return;
                }

                _activeGear = value;
            }
        }

        private void ApplyGearSprite(string gearSpriteReference)
        {
            ReferenceImage.ApplySprite("prnd", _backgroundSprite, false);
            ReferenceImage.ApplySprite(gearSpriteReference, _arrowSprite, true, true);
        }
    }
}
