#if UNITY_EDITOR
using Assets.Scripts.Camera;
using UnityEngine;
using UnityEditor;

namespace Assets.Scripts.System
{
    // Small helper script to attach an AudioListener to the scene view camera so that sound plays correctly in Scene View.
    [ExecuteInEditMode]
    public class SceneViewAudioHelper : MonoBehaviour
    {
        private AudioListener _audioListener;
        private UnityEngine.Camera _sceneCamera;
        private UnityEngine.Camera _lastCamera;

        private void Awake()
        {
            _sceneCamera = ((SceneView)SceneView.sceneViews[0]).camera;
            _audioListener = _sceneCamera.gameObject.AddComponent<AudioListener>();
            _audioListener.enabled = false;
        }

        private void Update()
        {
            UnityEngine.Camera currentCamera = UnityEngine.Camera.current;
            if (currentCamera != null)
            {
                _lastCamera = currentCamera;
            }

            if (_lastCamera == _sceneCamera || currentCamera == _sceneCamera)
            {
                CameraManager.Instance.AudioEnabled = false;
                _audioListener.enabled = true;
            }
            else
            {
                _audioListener.enabled = false;
                CameraManager.Instance.AudioEnabled = true;
            }
        }
    }
}
#endif