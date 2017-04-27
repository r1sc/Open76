using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class Car : MonoBehaviour
{
    public float SteerAngle;
    public float Throttle;

    public Wheel[] FrontWheels;
    public Wheel[] BackWheels;

    void Start()
    {
        
    }

    void FixedUpdate()
    {
        foreach (var frontWheel in FrontWheels)
        {
            frontWheel.WheelColider.steerAngle = SteerAngle;
        }
        foreach (var backWheel in BackWheels)
        {
            backWheel.WheelColider.motorTorque = Throttle*100;
        }
    }
}