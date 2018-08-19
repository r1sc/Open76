using System.Collections.Generic;
using Assets.Fileparsers;
using Assets.System;
using UnityEngine;

namespace Assets.Scripts.System
{
    public class SpriteManager
    {
        private static SpriteManager _instance;
        public static SpriteManager Instance
        {
            get { return _instance ?? (_instance = new SpriteManager()); }
        }

        private Dictionary<string, I76Sprite> _sprites;
        private Vdf _vdf;
        private CacheManager _cacheManager;
        
        public bool Initialised { get; private set; }

        private bool TryGetMapTexture(string mapName, out Texture2D mapTexture, out ETbl etbl)
        {
            List<ETbl> etbls = _vdf.Etbls;
            if (etbls == null)
            {
                mapTexture = null;
                etbl = null;
                return false;
            }

            int etblCount = etbls.Count;
            for (int i = 0; i < etblCount; ++i)
            {
                etbl = _vdf.Etbls[i];
                if (etbl.MapFile == mapName)
                {
                    mapTexture = TextureParser.ReadMapTexture(mapName, _cacheManager.Palette);
                    return true;
                }
            }

            Debug.LogErrorFormat("Map texture '{0}' not found.", mapName);
            mapTexture = null;
            etbl = null;
            return false;
        }

        public I76Sprite GetDiodeSprite(int healthGroup)
        {
            switch (healthGroup)
            {
                case 0:
                    return GetSprite("zdde.map", "off");
                case 1:
                    return GetSprite("zdde.map", "green");
                case 2:
                    return GetSprite("zdde.map", "yellow");
                case 3:
                    return GetSprite("zdde.map", "red");
                case 4:
                    return GetSprite("zdde.map", "drk");
                default:
                    Debug.LogWarning("Invalid health group requested in GetDiodeSprite - " + healthGroup);
                    return null;
            }
        }

        public I76Sprite GetNumberSprite(char number)
        {
            return GetSprite("znbe.map", number.ToString());
        }

        public ReferenceImage LoadReferenceImage(string mapName)
        {
            ETbl etbl;
            Texture2D mapTexture;
            if (!TryGetMapTexture(mapName, out mapTexture, out etbl))
            {
                return null;
            }

            return new ReferenceImage(mapName, mapTexture, etbl.Items);
        }

        public I76Sprite GetSprite(string mapName, string spriteName, int xOffset = 0, int yOffset = 0, bool addToCache = true)
        {
            I76Sprite sprite;
            if (addToCache && _sprites.TryGetValue(spriteName, out sprite))
            {
                return sprite;
            }
            
            ETbl etbl;
            Texture2D mapTexture;
            if (!TryGetMapTexture(mapName, out mapTexture, out etbl))
            {
                return null;
            }

            if (etbl.IsReferenceImage)
            {
                Debug.LogErrorFormat("Map '{0}' is a reference image, not a sprite sheet.", mapName);
                return null;
            }

            ETbl.ETblItem item;
            if (!etbl.Items.TryGetValue(spriteName, out item))
            {
                Debug.LogErrorFormat("Sprite '{0}' does not exist in map '{1}'.", spriteName, mapName);
                return null;
            }

            sprite = new I76Sprite();
            int mapYOffset = Mathf.Clamp(mapTexture.height - item.Height - item.YOffset - item.Height * yOffset + 1, 0, mapTexture.height - item.Height);
            Color[] pixels = mapTexture.GetPixels(item.XOffset + item.Width * xOffset, mapYOffset, item.Width, item.Height, 0);
            sprite.Pixels = pixels;
            sprite.Width = item.Width;
            sprite.Height = item.Height;

            if (addToCache)
            {
                _sprites.Add(spriteName, sprite);
            }

            return sprite;
        }

        public void Initialise(Vdf vdf)
        {
            if (Initialised)
            {
                return;
            }

            _vdf = vdf;
            _cacheManager = Object.FindObjectOfType<CacheManager>();

            // Initialise with common sprites using more convenient names.
            _sprites = new Dictionary<string, I76Sprite>
            {
                // Numbers
                {"1", GetSprite("znbe.map", "top", 0, 1, false)},
                {"2", GetSprite("znbe.map", "top", 0, 2, false)},
                {"3", GetSprite("znbe.map", "top", 0, 3, false)},
                {"4", GetSprite("znbe.map", "top", 0, 4, false)},
                {"5", GetSprite("znbe.map", "top", 0, 5, false)},
                {"6", GetSprite("znbe.map", "top", 0, 6, false)},
                {"7", GetSprite("znbe.map", "top", 0, 7, false)},
                {"8", GetSprite("znbe.map", "top", 0, 8, false)},
                {"9", GetSprite("znbe.map", "top", 0, 9, false)},
                {"0", GetSprite("znbe.map", "top", 0, 0, false)}
            };
            
            Initialised = true;
        }
    }
}
