using Assets.Fileparsers;
using Assets.System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets
{
    public class MainMenu : MonoBehaviour
    {

        public string MissionFile;
        public string VcfToLoad;

        // Use this for initialization
        void Start()
        {
            var levelLoader = GetComponent<LevelLoader>();
            levelLoader.LoadLevel(MissionFile);

            Vdf unused;
            var cacheManager = CacheManager.Instance;
            var importedVcf = cacheManager.ImportVcf(VcfToLoad, false, out unused);
            importedVcf.transform.position = new Vector3(339, 0.2f, 325);
            importedVcf.transform.localRotation = Quaternion.Euler(0, 96, 0);

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}