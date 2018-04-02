using Assets.Car;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Assets
{
    [RequireComponent(typeof(SmoothFollow))]
    public class CameraController : MonoBehaviour
    {
        private Camera _camera;
        private SmoothFollow _smoothFollow;

        private enum ChassisView
        {
            FirstPerson,
            ThirdPerson,
            AllHidden
        }

        // Use this for initialization
        void Start()
        {
            _camera = GetComponent<Camera>();
            _smoothFollow = GetComponent<SmoothFollow>();
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                SetCameraFirstPersonAtVLOCIndex(0);
                SetVisibleChassisModel(ChassisView.FirstPerson);
            }
            else if (Input.GetKeyDown(KeyCode.F2))
            {
                SetCameraThirdPerson();
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
        }
    }
}