using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Suspension : MonoBehaviour
{
    public float Length;
    public float WheelRadius;
    public float SpringConstant;
    public float DampingConstant;
    public float SideForceAmount = 10;
    public LayerMask RaycastLayerMask;

    public float Compression;
    public bool Grounded;

    public float SteerAmount;
    public Transform WheelGraphic;

    public float AccelerationForceMagnitude;
    public float BrakeForceMagnitude;
    public float Throttle;
    public float Brake;

    private Rigidbody _rigidbody;
    private float _previousCompression;

    // Use this for initialization
    void Start()
    {
        _rigidbody = GetComponentInParent<Rigidbody>();
    }

    void OnDrawGizmos()
    {
        var floor = -transform.up * (Length - Compression);
        Gizmos.color = Compression == 0 ? Color.blue : Color.white;
        Gizmos.DrawLine(transform.position, transform.position + floor);
        Gizmos.color = Color.red;
        Gizmos.DrawCube(transform.position + floor, Vector3.one * 0.1f);
    }

    void Update()
    {
        var steerAngle = SteerAmount * 40;
        transform.localRotation = Quaternion.AngleAxis(steerAngle, Vector3.up);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        RaycastHit hit;
        var realLength = Length + WheelRadius;
        Grounded = Physics.Raycast(transform.position, -transform.up, out hit, realLength, RaycastLayerMask.value);
        if (Grounded)
        {
            Compression = Mathf.Clamp01(realLength - hit.distance);
            //Compression = hit.distance / realLength;
            //Compression = -Compression + 1;
            
            //var springForce = transform.up * Compression * SpringConstant;
            //var dampingForce = -transform.up * (_previousCompression - Compression) * DampingConstant;
            var springForce = -transform.up * -(_rigidbody.mass / (Time.deltaTime * Time.deltaTime)) * SpringConstant * Compression;
            var dampingForce = transform.up * -(_rigidbody.mass / Time.deltaTime) * DampingConstant * Mathf.Max(0, _previousCompression - Compression);

            var localVelocity = transform.InverseTransformVector(_rigidbody.GetPointVelocity(transform.position));
            var sideForce = -transform.right * localVelocity.x * SideForceAmount;

            var accelerationForce = transform.forward * AccelerationForceMagnitude * Throttle;
            var brakeForce = -transform.forward * BrakeForceMagnitude * Brake;

            _rigidbody.AddForceAtPosition(springForce + dampingForce + sideForce, transform.position);
            _rigidbody.AddForceAtPosition(accelerationForce + brakeForce, transform.position -transform.up * realLength);
        }
        else
        {
            Compression = 0;
        }

        _previousCompression = Compression;
    }

    void LateUpdate()
    {
        if (WheelGraphic == null)
            return;
        var pos = WheelGraphic.localPosition;
        pos.y = -(Length - Compression);
        WheelGraphic.localPosition = pos;
    }
}
