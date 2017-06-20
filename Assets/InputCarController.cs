using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class InputCarController : MonoBehaviour
{
    private ArcadeCar _car;

    void Start()
    {
        _car = GetComponent<ArcadeCar>();
    }

    void Update()
    {
        var throttle = Input.GetAxis("Vertical");
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

