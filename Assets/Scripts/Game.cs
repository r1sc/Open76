using Assets.System;
using UnityEngine;

namespace Assets
{
    public class Game : MonoBehaviour
    {
        public string MissionFile;
        public string VcfToLoad;

        // Use this for initialization
        void Start()
        {
            var levelLoader = GetComponent<LevelLoader>();
            levelLoader.LoadLevel(MissionFile);

            if (MissionFile.ToLower().StartsWith("m"))
            {
                var cacheManager = FindObjectOfType<CacheManager>();
                var importedVcf = cacheManager.ImportVcf(VcfToLoad, true);
                importedVcf.AddComponent<InputCarController>();

                var spawnPoint = GameObject.FindGameObjectsWithTag("Spawn")[0];
                importedVcf.transform.position = spawnPoint.transform.position;
                importedVcf.transform.rotation = spawnPoint.transform.rotation;

                FindObjectOfType<SmoothFollow>().Target = importedVcf.transform;
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}