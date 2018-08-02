using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Camera
{
    public class CameraManager
    {
        private readonly Stack<UnityEngine.Camera> _cameraStack;
        private readonly GameObject _mainCameraObject;

        public UnityEngine.Camera MainCamera
        {
            get { return _mainCameraObject != null ? _mainCameraObject.GetComponent<UnityEngine.Camera>() : null; }
        }

        public UnityEngine.Camera ActiveCamera
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

        public bool IsMainCameraActive
        {
            get { return MainCamera == ActiveCamera; }
        }

        private CameraManager()
        {
            _cameraStack = new Stack<UnityEngine.Camera>();
            UnityEngine.Camera mainCamera = Object.FindObjectOfType<UnityEngine.Camera>();
            _mainCameraObject = mainCamera.gameObject;
            _cameraStack.Push(mainCamera);
        }
        
        public void PushCamera()
        {
            if (_cameraStack.Count > 0)
            {
                _cameraStack.Peek().enabled = false;
            }

            GameObject newCameraObject = new GameObject("Stack Camera " + _cameraStack.Count);
            UnityEngine.Camera newCamera = newCameraObject.AddComponent<UnityEngine.Camera>();
            _cameraStack.Push(newCamera);
        }

        public void PopCamera()
        {
            if (_cameraStack.Count == 0)
            {
                return;
            }

            UnityEngine.Camera stackCamera = _cameraStack.Pop();
            Object.Destroy(stackCamera.gameObject);

            if (_cameraStack.Count > 0)
            {
                _cameraStack.Peek().enabled = true;
            }
        }

        public void Destroy()
        {
            while (_cameraStack.Count > 0)
            {
                UnityEngine.Camera stackCamera = _cameraStack.Pop();
                Object.Destroy(stackCamera.gameObject);
            }

            _instance = null;
        }
    }
}
