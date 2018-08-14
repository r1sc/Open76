using Assets.Fileparsers;
using Assets.Scripts.Camera;
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
                Vdf unused;
                var cacheManager = FindObjectOfType<CacheManager>();
                var importedVcf = cacheManager.ImportVcf(VcfToLoad, true, out unused);
                importedVcf.AddComponent<InputCarController>();
                importedVcf.AddComponent<CarAI>();

                var spawnPoint = GameObject.FindGameObjectsWithTag("Spawn")[0];
                importedVcf.transform.position = spawnPoint.transform.position;
                importedVcf.transform.rotation = spawnPoint.transform.rotation;

                CameraManager.Instance.MainCamera.GetComponent<SmoothFollow>().Target = importedVcf.transform;
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}