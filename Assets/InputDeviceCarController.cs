using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class InputDeviceCarController : MonoBehaviour
{
    private Car _car;

    void Start()
    {
        _car = GetComponent<Car>();
    }

    void Update()
    {
        var horizontal = Input.GetAxis("Horizontal");
        _car.SteerAngle = horizontal * 45;
        var throttle = Input.GetAxis("Fire1");
        _car.Throttle = throttle;
    }
}

