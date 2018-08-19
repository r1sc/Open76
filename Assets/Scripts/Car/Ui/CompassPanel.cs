using Assets.Scripts.System;
using Assets.System;
using UnityEngine;

namespace Assets.Scripts.Car.Ui
{
    public class CompassPanel : Panel
    {
        private const float MinimumBearingChangeForTextureUpdate = 2.0f;
        private const int TextureOverlapFix = 5; // The compass letters don't line up exactly, small manual fix.

        private readonly I76Sprite _compassSprite;
        private readonly Color[] _bearingsPixels;
        private float _lastHeading;
        private readonly float _degreesPerPixel;
        private readonly int _overlapWrapValue;
        private readonly int _textureWidth;

        public CompassPanel(Transform firstPersonTransform) : base(firstPersonTransform, "CMP", "zcm_.map")
        {
            if (ReferenceImage == null)
            {
                return;
            }

            // The compass is a bit special since it smoothly scrolls, so we just load the entire frame instead of the actual sprite.
            CacheManager cacheManager = Object.FindObjectOfType<CacheManager>();
            Texture2D bearingsTexture = cacheManager.GetTexture("zcme");
            if (bearingsTexture == null)
            {
                Debug.LogError("Failed to load compass texture.");
                return;
            }

            _textureWidth = bearingsTexture.width;
            _bearingsPixels = bearingsTexture.GetPixels();
            _compassSprite = SpriteManager.GetSprite("zcme.map", "left", 0, 0, false);
            _overlapWrapValue = _textureWidth - _compassSprite.Width - TextureOverlapFix;
            _degreesPerPixel = 360f / _overlapWrapValue;

            UpdateCompassHeading(0f);
        }

        public void UpdateCompassHeading(float heading)
        {
            if (Mathf.Abs(heading - _lastHeading) < MinimumBearingChangeForTextureUpdate)
            {
                return;
            }
            _lastHeading = heading;
            
            int xStart = (int)(heading / _degreesPerPixel);
            int xEnd = xStart + _compassSprite.Width;

            for (int y = 0; y < _compassSprite.Height; ++y)
            {
                int spriteYLength = _compassSprite.Width * y;
                int pixelYLength = _textureWidth * y;
                for (int x = xStart; x < xEnd; ++x)
                {
                    int spriteIndex = x - xStart + spriteYLength;
                    int pixelIndex = ( x) % _overlapWrapValue + pixelYLength;
                    _compassSprite.Pixels[spriteIndex] = _bearingsPixels[pixelIndex];
                }
            }

            ReferenceImage.ApplySprite("compass_window", _compassSprite, true);
        }
    }
}
