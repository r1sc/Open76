using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class Car : MonoBehaviour
{
    public float SteerAngle;
    public float Throttle;

    public WheelCollider[] FrontWheels;
    public WheelCollider[] BackWheels;
    public float Torque;
    private Rigidbody _rigidbody;

    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.centerOfMass += -Vector3.up * 0.5f;

    }

    void Update()
    {
        var horizontal = Input.GetAxis("Horizontal");
        Throttle = Input.GetAxis("Throttle");
        SteerAngle = horizontal*45;
    }

    void FixedUpdate()
    {
        foreach (var frontWheel in FrontWheels)
        {
            frontWheel.steerAngle = SteerAngle;
        }
        foreach (var backWheel in BackWheels)
        {
            backWheel.motorTorque = Throttle*Torque;
        }
    }
}