using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ArcadeCar : MonoBehaviour
{
    private Rigidbody _rigidbody;
    public float DownForceAmount;
    public ArcadeWheel[] SteerWheels, DriveWheels;

    private ArcadeWheel[] _wheels;
    private float _wheelBase;

    public float CornerStiffnessFront = 5.0f;
    public float CornerStiffnessRear = 5.2f;
    public float RollingResistance = 30.0f;
    public float MaxGripFront = 2.0f;
    public float MaxGripRear = 2.0f;
    public bool FrontSlip = false;
    public bool RearSlip = false;
    public float WeightTransfer = 0.2f;
    public float AccelerationForce;

    // Use this for initialization
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        //_rigidbody.centerOfMass -= Vector3.up * 0.5f;

        _wheels = GetComponentsInChildren<ArcadeWheel>();

        _wheelBase = 2.0f;
            //Mathf.Abs(SteerWheels[0].transform.localPosition.z - DriveWheels[0].transform.localPosition.z);
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnDrawGizmos()
    {
        if (_rigidbody != null)
            Gizmos.DrawSphere(_rigidbody.worldCenterOfMass, 0.5f);
    }

    //private float yawrate = 0;
    void FixedUpdate()
    {
        var velocity = transform.InverseTransformVector(_rigidbody.velocity);

        var throttle = Input.GetAxis("Throttle");
        var brake = -Mathf.Min(0, throttle);
        throttle = Mathf.Max(0, throttle);

        var steering = Input.GetAxis("Horizontal");
        var steerDegrees = Mathf.Lerp(steering*40, steering * 5, velocity.magnitude / 30.0f);

        MaxGripRear = velocity.z > 20.0f ? 30 : 5;

        foreach (var steerWheel in SteerWheels)
        {
            steerWheel.SteerAngle = steering * 60;
        }

        var steerangle = steerDegrees * Mathf.Deg2Rad;

        var cgToFrontAxle = 1f;//Mathf.Abs(_rigidbody.centerOfMass.z - SteerWheels[0].transform.localPosition.z);
        var cgToRearAxle = 1f;//Mathf.Abs(_rigidbody.centerOfMass.z - DriveWheels[0].transform.localPosition.z);
        var axleWeightRatioFront = cgToRearAxle/_wheelBase;
        var axleWeightRatioRear = cgToFrontAxle/_wheelBase;

        var axleWeightFront = _rigidbody.mass * (axleWeightRatioFront * 9.81f - WeightTransfer * throttle * 0.5f / _wheelBase);
        var axleWeightRear = _rigidbody.mass * (axleWeightRatioRear * 9.81f + WeightTransfer * throttle * 0.5f / _wheelBase);

        var yawrate = _rigidbody.angularVelocity.y;
        var yawSpeedFront = 1.0f * yawrate;
        var yawSpeedRear = -1.0f * yawrate;

        var slipAngleFront = Mathf.Atan2(velocity.x + yawSpeedFront, Mathf.Abs(velocity.z)) - Mathf.Sign(velocity.z) * steerangle;
        var slipAngleRear = Mathf.Atan2(velocity.x + yawSpeedRear, Mathf.Abs(velocity.z));

        var tireGripFront = MaxGripFront;
        var tireGripRear = MaxGripRear;

        var frictionForceFront = Mathf.Clamp(-CornerStiffnessFront * slipAngleFront, -tireGripFront, tireGripFront) * axleWeightFront;
        var frictionForceRear = Mathf.Clamp(-CornerStiffnessRear * slipAngleRear, -tireGripRear, tireGripRear) * axleWeightRear;

        var tractionForce = Vector3.zero;
        if(DriveWheels.Any(x => x.Grounded))
            tractionForce = transform.forward * (throttle - brake * Mathf.Sign(velocity.z)) * AccelerationForce;
        
        var sideForce = transform.right * (Mathf.Cos(steerangle) * frictionForceFront + frictionForceRear);

        var downForce = -transform.up * DownForceAmount;

        var driveForce = DriveWheels.Any(x => x.Grounded) ? tractionForce : Vector3.zero;
        var actualSideForce = ((DriveWheels[0].Grounded && SteerWheels[0].Grounded) ||
                              (DriveWheels[1].Grounded && SteerWheels[1].Grounded)) ? sideForce : Vector3.zero;

        
        _rigidbody.AddForce(driveForce + actualSideForce + downForce);
        //_rigidbody.AddForceAtPosition(downForce, transform.position - transform.forward * (DriveWheels.All(x => x.Grounded) ? (throttle-brake) * 0.5f : 0.0f));
        
        var angularTorque = frictionForceFront - frictionForceRear;

        var angularAccel = angularTorque / _rigidbody.mass;
        if (SteerWheels.All(x => x.Grounded))
        {
            _rigidbody.angularVelocity += transform.up*angularAccel*Time.deltaTime;
        }
    }
}
