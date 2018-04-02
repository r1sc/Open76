using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Assets.Fileparsers;
using Assets.System;

namespace Assets
{
    public class SdfViewer : MonoBehaviour
    {
        public GameObject SDFContainer;
        private CacheManager _cacheManager;
        public string[] SdfFiles;

        public Transform ButtonPrefab;
        public Transform ListTarget;

        // Use this for initialization
        void Start()
        {
            _cacheManager = FindObjectOfType<CacheManager>();
            _cacheManager.Palette = ActPaletteParser.ReadActPalette("t01.act");

            SdfFiles = VirtualFilesystem.Instance.FindAllWithExtension(".sdf").ToArray();

            foreach (var sdfFile in SdfFiles)
            {
                var filename = sdfFile;
                var button = Instantiate(ButtonPrefab, Vector3.zero, Quaternion.identity, ListTarget).GetComponent<Button>();
                button.GetComponentInChildren<Text>().text = filename;
                button.onClick.AddListener(new UnityEngine.Events.UnityAction(() =>
                {
                    OnClickButton(filename);
                }));
            }
        }

        private void OnClickButton(string filename)
        {
            LoadSDF(filename);
        }

        // Update is called once per frame
        void Update()
        {

        }

        void LoadSDF(string filename)
        {
            if (SDFContainer != null)
                Destroy(SDFContainer);

            SDFContainer = _cacheManager.ImportSdf(filename, transform, Vector3.zero, Quaternion.identity);
            _cacheManager.ClearCache();
        }
    }
}