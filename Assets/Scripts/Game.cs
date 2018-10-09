﻿using Assets.Fileparsers;
using Assets.Scripts.Camera;
using Assets.Scripts.Car;
using Assets.Scripts.System;
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
                var cacheManager = CacheManager.Instance;
                var importedVcf = cacheManager.ImportVcf(VcfToLoad, true);
                importedVcf.AddComponent<InputCarController>();
                importedVcf.AddComponent<CarController>();

                var spawnPoint = GameObject.FindGameObjectsWithTag("Spawn")[0];
                importedVcf.transform.position = spawnPoint.transform.position;
                importedVcf.transform.rotation = spawnPoint.transform.rotation;

                CameraManager.Instance.MainCamera.GetComponent<SmoothFollow>().Target = importedVcf.transform;
            }

#if UNITY_EDITOR
            gameObject.AddComponent<SceneViewAudioHelper>();
#endif
        }
    }
}