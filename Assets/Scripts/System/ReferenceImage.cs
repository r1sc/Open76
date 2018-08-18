using System.Collections.Generic;
using Assets.Fileparsers;
using UnityEngine;

namespace Assets.Scripts.System
{
    public class ReferenceImage
    {
        public Texture2D MainTexture { get; private set; }
        public string Name { get; private set; }

        private readonly Dictionary<string, Vector2Int> _referencePositions;

        public ReferenceImage(string name, Texture2D mainTexture, Dictionary<string, ETbl.ETblItem> etblItems)
        {
            Name = name;
            MainTexture = mainTexture;

            _referencePositions = new Dictionary<string, Vector2Int>(etblItems.Count);
            foreach (KeyValuePair<string, ETbl.ETblItem> item in etblItems)
            {
                ETbl.ETblItem itemValue = item.Value;
                Vector2Int referencePosition = new Vector2Int(itemValue.XOffset, itemValue.YOffset);
                _referencePositions.Add(item.Key, referencePosition);
            }
        }

        public void ApplySprite(string referenceId, I76Sprite sprite, bool uploadToGpu)
        {
            if (sprite == null)
            {
                return;
            }

            Vector2Int referencePos;
            if (!_referencePositions.TryGetValue(referenceId, out referencePos))
            {
                Debug.LogErrorFormat("Reference ID '{0}' does not exist for reference image '{1}'.", referenceId, Name);
                return;
            }
            
            MainTexture.SetPixels(referencePos.x, MainTexture.height - referencePos.y - sprite.Height, sprite.Width, sprite.Height, sprite.Pixels, 0);

            if (uploadToGpu)
            {
                MainTexture.Apply();
            }
        }

        public void UploadToGpu()
        {
            MainTexture.Apply();
        }
    }

    public class I76Sprite
    {
        public Color[] Pixels { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
