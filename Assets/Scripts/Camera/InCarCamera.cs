using Assets.Car;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets
{
    class InCarCamera : MonoBehaviour
    {
        private NewCar _car;

        void Start()
        {
            _car = GetComponentInParent<NewCar>();
        }

        void Update()
        {
            var rotX = (_car.Throttle - _car.Brake) * 4;
            var rotZ = _car.SteerAngle * Mathf.Rad2Deg * 2;
            transform.localRotation = Quaternion.AngleAxis(-rotX, Vector3.right) * Quaternion.AngleAxis(rotZ, Vector3.forward);
        }
    }
}