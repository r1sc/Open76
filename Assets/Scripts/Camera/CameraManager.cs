using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Camera
{
    public class CameraManager
    {
        private readonly Stack<FSMCamera> _cameraStack;
        private readonly GameObject _mainCameraObject;
        private bool _audioEnabled;

        public UnityEngine.Camera MainCamera
        {
            get { return _mainCameraObject != null ? _mainCameraObject.GetComponent<UnityEngine.Camera>() : null; }
        }

        public FSMCamera ActiveCamera
        {
            get
            {
                if (_cameraStack.Count > 0)
                {
                    return _cameraStack.Peek();
                }

                return null;
            }
        }

        private static CameraManager _instance;

        public static CameraManager Instance
        {
            get { return _instance ?? (_instance = new CameraManager()); }
        }

        public bool AudioEnabled
        {
            get { return _audioEnabled; }
            set
            {
                if (_audioEnabled == value)
                {
                    return;
                }

                _audioEnabled = value;
                if (_cameraStack.Count > 0)
                {
                    var camera = _cameraStack.Peek();
                    camera.GetComponent<AudioListener>().enabled = value;
                }
                else
                {
                    MainCamera.GetComponent<AudioListener>().enabled = value;
                }
            }
        }

        public bool IsMainCameraActive
        {
            get { return MainCamera == ActiveCamera; }
        }

        private CameraManager()
        {
            _cameraStack = new Stack<FSMCamera>();
            var mainCamera = Object.FindObjectOfType<FSMCamera>();
            _mainCameraObject = mainCamera.gameObject;
            _cameraStack.Push(mainCamera);
            _audioEnabled = true;
        }
        
        public void PushCamera()
        {
            if (_cameraStack.Count > 0)
            {
                var camera = _cameraStack.Peek();
                camera.gameObject.SetActive(false);
            }

            GameObject newCameraObject = new GameObject("Stack Camera " + _cameraStack.Count);
            newCameraObject.AddComponent<UnityEngine.Camera>();
            var newCamera = newCameraObject.AddComponent<FSMCamera>();
            newCameraObject.AddComponent<AudioListener>();
            _cameraStack.Push(newCamera);
        }

        public void PopCamera()
        {
            if (_cameraStack.Count == 0)
            {
                return;
            }

            var stackCamera = _cameraStack.Pop();
            Object.Destroy(stackCamera.gameObject);

            if (_cameraStack.Count > 0)
            {
                var camera = _cameraStack.Peek();
                camera.gameObject.SetActive(false);
            }
        }

        public void Destroy()
        {
            while (_cameraStack.Count > 0)
            {
                var stackCamera = _cameraStack.Pop();
                Object.Destroy(stackCamera.gameObject);
            }

            _instance = null;
        }
    }
}
