using System.Linq;
using Assets.Scripts.System;
using Assets.Scripts.System.Fileparsers;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class SdfViewer : MonoBehaviour
    {
        public GameObject SDFContainer;
        private CacheManager _cacheManager;
        public string[] SdfFiles;

        public Transform ButtonPrefab;
        public Transform ListTarget;

        // Use this for initialization
        private void Start()
        {
            _cacheManager = CacheManager.Instance;
            _cacheManager.Palette = ActPaletteParser.ReadActPalette("t01.act");

            SdfFiles = VirtualFilesystem.Instance.FindAllWithExtension(".sdf").ToArray();

            foreach (string sdfFile in SdfFiles)
            {
                string filename = sdfFile;
                Button button = Instantiate(ButtonPrefab, Vector3.zero, Quaternion.identity, ListTarget).GetComponent<Button>();
                button.GetComponentInChildren<Text>().text = filename;
                button.onClick.AddListener(() =>
                {
                    OnClickButton(filename);
                });
            }
        }

        private void OnClickButton(string filename)
        {
            LoadSDF(filename);
        }

        private void LoadSDF(string filename)
        {
            if (SDFContainer != null)
                Destroy(SDFContainer);

            SDFContainer = _cacheManager.ImportSdf(filename, transform, Vector3.zero, Quaternion.identity, false, out _, out _);
            _cacheManager.ClearCache();
        }
    }
}