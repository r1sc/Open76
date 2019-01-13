using System.Collections;
using Assets.Scripts.Camera;
using Assets.Scripts.CarSystems;
using Assets.Scripts.Entities;
using Assets.Scripts.System;
using Assets.Scripts.System.Fileparsers;
using UnityEngine;

namespace Assets.Scripts
{
    public class SceneRoot : MonoBehaviour
    {
        public string GamePath;
        public string MissionFile;
        public string VcfToLoad;

        private IEnumerator Start()
        {
#if UNITY_EDITOR
            gameObject.AddComponent<SceneViewAudioHelper>();
#endif

            if (!string.IsNullOrEmpty(Game.Instance.LevelName))
            {
                MissionFile = Game.Instance.LevelName;
            }

            if (string.IsNullOrEmpty(Game.Instance.GamePath))
            {
                Game.Instance.GamePath = GamePath;
            }

            yield return LevelLoader.Instance.LoadLevel(MissionFile);
            
            if (MissionFile.ToLower().StartsWith("m"))
            {
                CacheManager cacheManager = CacheManager.Instance;
                GameObject importedVcf = cacheManager.ImportVcf(VcfToLoad, true, out Vdf unused);
                importedVcf.AddComponent<CarInput>();
                importedVcf.AddComponent<Car>();

                GameObject spawnPoint = GameObject.FindGameObjectsWithTag("Spawn")[0];
                importedVcf.transform.position = spawnPoint.transform.position;
                importedVcf.transform.rotation = spawnPoint.transform.rotation;

                CameraManager.Instance.MainCamera.GetComponent<SmoothFollow>().Target = importedVcf.transform;
            }
        }
    }
}