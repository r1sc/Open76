using UnityEngine;
using System.Collections;

public class RayWheel : MonoBehaviour
{
    //public Transform WheelModel;
    public float SpringConstant = 100;
    public float Damping = 100;
    public float WheelRadius = 0.5f;
    public float SpringLength = 1;
    public bool Grounded;
    public AnimationCurve SlipRatio;    // x = velocity, y = force
    public float SlipForceAmount = 100;
    private Rigidbody _rigidbody;
    private float _lastSpringLength = 0;
    private Transform _wheelGraphic;

    // Use this for initialization
    [ExecuteInEditMode]
    void Start()
    {
        _rigidbody = GetComponentInParent<Rigidbody>();
        _lastSpringLength = SpringLength;
        _wheelGraphic = transform.GetChild(0);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + -transform.up * SpringLength);
        Gizmos.DrawWireSphere(transform.GetChild(0).position, WheelRadius);
    }

    void FixedUpdate()
    {
        SpringPhysics();
        TestSidewaysFriction();
        //RollingResistance();
    }

    private void TestSidewaysFriction()
    {
        if (Grounded)
        {
            var wheelVelocity = _rigidbody.GetPointVelocity(transform.position);
            var localWheelVelocity = transform.InverseTransformVector(wheelVelocity);

            var angle = Mathf.Abs(localWheelVelocity.normalized.x * 90);
            if (localWheelVelocity.magnitude < 0.1f)
                angle = 0;

            Debug.DrawLine(transform.position, transform.position + transform.TransformDirection(localWheelVelocity.normalized), Color.yellow);

            var corneringForce = (SlipRatio.Evaluate(Mathf.Abs(angle)) * SlipForceAmount) / _rigidbody.mass;
            corneringForce = Mathf.Min(Mathf.Abs(localWheelVelocity.x), corneringForce);
            
            // var corneringForce = (1.0f - slip) * SlipForceAmount;
            // var corneringForce = Mathf.Abs(localWheelVelocity.normalized.x) * SlipForceAmount;

            var force = -transform.right * corneringForce * Mathf.Sign(localWheelVelocity.x);
            Debug.DrawLine(transform.position, transform.position + force, Color.blue);
            _rigidbody.AddForceAtPosition(force, transform.position, ForceMode.Acceleration);

        }
    }

    private void RollingResistance()
    {
        var rollingResistance = _rigidbody.drag * 30;
        var wheelVelocity = _rigidbody.GetPointVelocity(transform.position);
        var localVelocity = transform.InverseTransformVector(wheelVelocity);

        wheelVelocity.y = 0;
        if (localVelocity.z < 1 || localVelocity.z > 1)
        {
            var fRoll = -rollingResistance * Mathf.Sign(localVelocity.z) * transform.forward;
            _rigidbody.AddForceAtPosition(fRoll, transform.position, ForceMode.Force);
            Debug.DrawLine(transform.position, transform.position + fRoll, Color.magenta);
        }
    }

    private void SpringPhysics()
    {
        RaycastHit rayHit;
        var springNow = SpringLength;
        if (Physics.Raycast(transform.position, -transform.up, out rayHit, SpringLength))
        {
            springNow = rayHit.distance;
            Grounded = true;
        }
        else
        {
            Grounded = false;
        }

        var displacement = SpringLength - springNow;
        var force = transform.up * SpringConstant * displacement;
        var springVel = springNow - _lastSpringLength;
        var wheelVel = springVel * transform.up;
        Debug.DrawLine(transform.position, transform.position + wheelVel * 10, Color.yellow);
        var damper = -Damping * wheelVel;
        force += damper;

        _rigidbody.AddForceAtPosition(force, transform.position, ForceMode.Force);
        Debug.DrawLine(transform.position, transform.position + force, Color.red);
        _lastSpringLength = springNow;

        var pos = _wheelGraphic.localPosition;
        pos.y = -_lastSpringLength + WheelRadius;
        _wheelGraphic.localPosition = pos;
    }
}
