using System;
using Assets.Car;
using UnityEngine;
using UnityEngine.XR;

namespace Assets
{
    [RequireComponent(typeof(SmoothFollow))]
    public class CameraController : MonoBehaviour
    {
        public LayerMask FirstPersonLayers;
        public LayerMask ThirdPersonLayers;

        private Camera _camera;
        private SmoothFollow _smoothFollow;
        private bool _firstPerson = false;

        private enum ChassisView
        {
            FirstPerson,
            ThirdPerson,
            AllHidden
        }

        // Use this for initialization
        void Start()
        {
            _camera = GetComponentInChildren<Camera>();
            _smoothFollow = GetComponent<SmoothFollow>();
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                SetCameraFirstPersonAtVLOCIndex(0);
                SetVisibleChassisModel(ChassisView.FirstPerson);
                _firstPerson = true;
            }
            else if (Input.GetKeyDown(KeyCode.F2))
            {
                SetCameraThirdPerson();
                _firstPerson = false;
            }
            else if (Input.GetKeyDown(KeyCode.F3))
            {
                SetCameraFirstPersonAtVLOCIndex(1);
                SetVisibleChassisModel(ChassisView.AllHidden);
                _firstPerson = false;
            }
            else if (Input.GetKeyDown(KeyCode.F4))
            {
                SetCameraAtWheelIndex(0);
                SetVisibleChassisModel(ChassisView.ThirdPerson);
                _firstPerson = false;
            }
            else if (Input.GetKeyDown(KeyCode.F5))
            {
                SetCameraAtWheelIndex(1);
                SetVisibleChassisModel(ChassisView.ThirdPerson);
                _firstPerson = false;
            }
            else if (Input.GetKeyDown(KeyCode.F6))
            {
                SetCameraAtWheelIndex(2);
                SetVisibleChassisModel(ChassisView.ThirdPerson);
                _firstPerson = false;
            }
            else if (Input.GetKeyDown(KeyCode.F7))
            {
                SetCameraAtWheelIndex(3);
                SetVisibleChassisModel(ChassisView.ThirdPerson);
                _firstPerson = false;
            }

            if(Input.GetKeyDown(KeyCode.R))
            {
                InputTracking.Recenter();
            }

            if (_firstPerson)
            {
                var targetRotation = Quaternion.Euler(-14, 0, 0);
                if (Input.GetKey(KeyCode.Keypad6))
                    targetRotation = Quaternion.Euler(-14, 90, 0);
                else if (Input.GetKey(KeyCode.Keypad2))
                    targetRotation = Quaternion.Euler(-14, 180, 0);
                else if (Input.GetKey(KeyCode.Keypad4))
                    targetRotation = Quaternion.Euler(-14, -90, 0);
                if (Input.GetKey(KeyCode.Keypad8))
                    targetRotation = Quaternion.Euler(7, 0, 0);

                transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * 6);
            }
        }

        private void SetCameraAtWheelIndex(int wheelIndex)
        {
            var inputCarController = FindObjectOfType<InputCarController>();
            var suspensions = inputCarController.GetComponentsInChildren<RaySusp>();
            if(wheelIndex < suspensions.Length)
            {
                var wheel = suspensions[wheelIndex].transform;
                var target = wheel.Find("Mesh").GetChild(0);

                transform.parent = wheel;
                transform.localPosition = target.localPosition;
                transform.localRotation = Quaternion.Euler(-14, 0, 0);
                _smoothFollow.enabled = false;
            }
        }

        private void SetCameraThirdPerson()
        {
            var inputCarController = FindObjectOfType<InputCarController>();
            _smoothFollow.Target = inputCarController.transform;
            _smoothFollow.enabled = true;
            transform.parent = null;

            SetVisibleChassisModel(ChassisView.ThirdPerson);
        }

        private void SetCameraFirstPersonAtVLOCIndex(int vlocIndex)
        {
            var inputCarController = FindObjectOfType<InputCarController>();

            int i = 0;
            Transform vloc = null;
            foreach (Transform child in inputCarController.transform)
            {
                if (child.name == "VLOC")
                {
                    if (i == vlocIndex)
                    {
                        vloc = child;
                    }
                    i++;
                }
            }

            if (vloc == null)
            {
                Debug.LogWarning("Cannot find VLOC with index " + vlocIndex);
                return;
            }

            transform.parent = vloc;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Euler(-14, 0, 0);
            _smoothFollow.enabled = false;
        }

        private void SetVisibleChassisModel(ChassisView chassisView)
        {
            var inputCarController = FindObjectOfType<InputCarController>();
            var thirdPerson = inputCarController.transform.Find("Chassis/ThirdPerson");
            thirdPerson.gameObject.SetActive(chassisView == ChassisView.ThirdPerson);

            var firstPerson = inputCarController.transform.Find("Chassis/FirstPerson");
            firstPerson.gameObject.SetActive(chassisView == ChassisView.FirstPerson);

            var suspensions = inputCarController.GetComponentsInChildren<RaySusp>();
            foreach (var suspension in suspensions)
            {
                suspension.SetWheelVisibile(chassisView == ChassisView.ThirdPerson);
            }            
        }
    }
}