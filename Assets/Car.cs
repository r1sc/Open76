using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Car : MonoBehaviour
{
    public WheelCollider[] SteerWheels;
    public WheelCollider[] DriveWheels;

    // Use this for initialization
	void Start ()
	{
        
	}
	
	// Update is called once per frame
	void Update ()
	{
	    var throttle = Input.GetAxis("Throttle");
	    var brake = -Mathf.Min(0, throttle);
	    throttle = Mathf.Max(0, throttle);

	    var horizontal = Input.GetAxis("Horizontal");
        
        foreach (var steerWheel in SteerWheels)
        {
            steerWheel.steerAngle = horizontal*40;
        }
        foreach (var driveWheel in DriveWheels)
        {
            driveWheel.motorTorque = throttle * 2000;
            driveWheel.brakeTorque = brake * 1000;
        }
    }
}
