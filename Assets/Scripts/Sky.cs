using Assets.Scripts.Camera;
using Assets.Scripts.System;
using Assets.Scripts.System.Fileparsers;
using UnityEngine;

namespace Assets.Scripts
{
    public class Sky : MonoBehaviour
    {
        public Vector2 Speed;
        public float Height;
        private Material _material;

        private string _textureFileName;
        public string TextureFilename
        {
            get { return _textureFileName;}
            set
            {
                if (_textureFileName == value)
                {
                    return;
                }

                _textureFileName = value;
                _material.mainTexture = TextureParser.ReadMapTexture(TextureFilename, CacheManager.Instance.Palette);
            }
        }

        private void Awake()
        {
            _material = GetComponent<MeshRenderer>().material;

            if (!string.IsNullOrEmpty(TextureFilename))
            {
                _material.mainTexture = TextureParser.ReadMapTexture(TextureFilename, CacheManager.Instance.Palette);
            }
        }

        private void Update()
        {
            _material.mainTextureOffset += Speed * Time.deltaTime;
            transform.position = CameraManager.Instance.ActiveCamera.transform.position + Vector3.up * Height;
        }
    }
}