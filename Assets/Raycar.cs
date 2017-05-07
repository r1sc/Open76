using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Raycar : MonoBehaviour
{
    public float Throttle;
    public float Torque;
    public float DownwardsSpringConstant;

    public Suspension[] SteerWheels;
    public Suspension[] DriveWheels;

    private Rigidbody _rigidbody;
    private Vector3 _centerOfMassAtRest;
    private Suspension[] _wheels;

    // Use this for initialization
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.centerOfMass += -Vector3.up * 0.5f;
        _centerOfMassAtRest = _rigidbody.centerOfMass;
        _wheels = GetComponentsInChildren<Suspension>();
    }

    void OnDrawGizmos()
    {
        if (_rigidbody == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.TransformPoint(_rigidbody.centerOfMass), 0.1f);
    }

    // Update is called once per frame
    void Update()
    {
        var horizontal = Input.GetAxis("Horizontal");
        Throttle = Input.GetAxis("Throttle");
        foreach (var steerWheel in SteerWheels)
        {
            steerWheel.SteerAmount = horizontal;
        }

    }

    void FixedUpdate()
    {
        Throttle = Mathf.Clamp(Throttle, -1, 1);
        var forwardForce = DriveWheels.All(x => x.Grounded) ? transform.forward * Torque * Throttle : Vector3.zero;
        var downForce = _wheels.Any(x => x.Grounded) ? -transform.up*DownwardsSpringConstant : Vector3.zero;

        _rigidbody.centerOfMass = _centerOfMassAtRest - Vector3.forward * Throttle * 0.5f;
        _rigidbody.AddForce(forwardForce + downForce);
    }
}
