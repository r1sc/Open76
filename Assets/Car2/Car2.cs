using UnityEngine;
using System.Collections;

public class Car2 : MonoBehaviour
{
    public RayWheel[] DriveWheels;
    public RayWheel[] BrakeWheels;
    public RayWheel[] SteeringWheels;
    public float EngineForce = 100.0f;
    public float BrakeForce = 100.0f;
    public float DownForce = 500.0f;

    public float Throttle, Brake, Steering;


    private Rigidbody _rigidbody;

    // Use this for initialization
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.centerOfMass += Vector3.down * 0.5f;
    }

    void Update()
    {
        var steerangle = Steering * 45;
        foreach (var wheel in SteeringWheels)
        {
            wheel.transform.localRotation = Quaternion.AngleAxis(steerangle, Vector3.up);
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        var engineForceAmount = 0.0f;
        var brakeForceAmount = 0.0f;
        if (Throttle > 0)
            engineForceAmount = Throttle*EngineForce;
        if (Brake > 0)
            brakeForceAmount = Brake*BrakeForce;

        if (engineForceAmount != 0)
        {
            foreach (var driveWheel in DriveWheels)
            {
                if (driveWheel.Grounded)
                {
                    _rigidbody.AddForceAtPosition(transform.forward * engineForceAmount, driveWheel.transform.position);
                }
            }
        }
        if (brakeForceAmount != 0)
        {
            foreach (var brakeWheel in BrakeWheels)
            {
                if (brakeWheel.Grounded)
                {
                    var wheelVelocity = brakeWheel.transform.InverseTransformVector(_rigidbody.GetPointVelocity(brakeWheel.transform.position));
                    if(wheelVelocity.magnitude != 0)
                        _rigidbody.AddForceAtPosition(-wheelVelocity.z * transform.forward * brakeForceAmount, brakeWheel.transform.position);
                }
            }
        }
        
        _rigidbody.AddForce(Vector3.down * DownForce);
    }
}
