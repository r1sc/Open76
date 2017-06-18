using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcadeWheel : MonoBehaviour
{

    public float SuspensionLength = 0.2f;
    public float SpringStiffness = 0.01f;
    public float Damping = 1;
    public float SuspensionTarget = 0.5f;
    public float WheelRadius = 0.3f;

    public float AccerationForce = 1000;
    public float BrakeForce = 1000;
    public float Throttle = 0;
    public Transform WheelGraphic;

    public float SteerAngle;
    public bool Grounded;

    private float _compression;
    private Rigidbody _rigidbody;
    private float _previousCompression;

    // Use this for initialization
    void Start()
    {
        _rigidbody = GetComponentInParent<Rigidbody>();
        if (WheelGraphic == null)
            WheelGraphic = transform.Find("Mesh");
    }


    void OnDrawGizmos()
    {
        var floor = -transform.up * (SuspensionLength - _compression);
        Gizmos.color = _compression == 0 ? Color.blue : Color.white;
        Gizmos.DrawLine(transform.position, transform.position + floor);
        Gizmos.color = Color.red;
        Gizmos.DrawCube(transform.position + floor, Vector3.one * 0.1f);
    }

    // Update is called once per frame
    void Update()
    {

    }

    void FixedUpdate()
    {
        RaycastHit hit;
        var realLength = SuspensionLength + WheelRadius;
        Grounded = Physics.Raycast(transform.position, -transform.up, out hit, realLength);
        if (Grounded)
        {
            _compression = Mathf.Clamp01(realLength - hit.distance);

            var springForce = transform.up * SpringStiffness * _compression*SuspensionTarget;
            var dampingForce = -transform.up * (_previousCompression - _compression) * Damping;
            
            _rigidbody.AddForceAtPosition(springForce + dampingForce, transform.position);
        }
        else
        {
            _compression = 0;
        }

        _previousCompression = _compression;
    }


    void LateUpdate()
    {
        if (WheelGraphic == null)
            return;
        var pos = WheelGraphic.localPosition;
        pos.y = -(SuspensionLength - _compression);
        WheelGraphic.localPosition = pos;
        WheelGraphic.localRotation = Quaternion.AngleAxis(SteerAngle, Vector3.up);
    }
}
