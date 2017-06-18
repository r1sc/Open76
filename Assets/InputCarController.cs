using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class InputCarController : MonoBehaviour
{
    private Car2 _car;

    void Start()
    {
        _car = GetComponent<Car2>();
    }

    void Update()
    {
        var throttle = Input.GetAxis("Throttle");
        var brake = -Mathf.Min(0, throttle);
        throttle = Mathf.Max(0, throttle);

        _car.Throttle = throttle;
        _car.Brake = brake;
        
        var steering = Input.GetAxis("Horizontal");
        _car.Steering = steering;

        var ebrake = Input.GetButton("E-brake");
        // _car.RearSlip = ebrake;
        if(ebrake)
            _car.Brake = 1;
    }
}

