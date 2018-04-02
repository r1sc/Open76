using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Car
{
    public class NewCar : MonoBehaviour
    {
        private Rigidbody _rigidbody;

        public float Throttle;
        public float Brake;
        public bool EBrake;
        public float Steer;
        public float MaxSteer;
        public float SteerSpeedBias = 250.0f;

        public float EngineForce;
        public float BrakeConstant;
        public float DragConstant;
        public float RollingResistanceConstant;
        public float WheelRadius = 0.33f;
        public RaySusp[] FrontWheels, RearWheels;
        public Transform Chassis;
        public float SteerAngle;
        public float CorneringStiffnessFront;
        public float CorneringStiffnessRear;
        public float MaxGrip;

        public float EngineMaxTorque;
        public float RPM;
        public float[] GearRatios;
        public float ReverseGearRatio;
        public float DifferentialRatio;

        private Vector2 _carVelocity;
        private float _speed;
        private float _weightFront;
        private float _weightRear;
        private Vector2 _carAcceleration;
        private float _wheelAngularVelocity;
        private float _percentFront;
        private float _totalWeight;
        private Vector2 _fTraction;
        private float _fTractionMax;
        private float _slipLongitudal;
        private float _b, _c;
        private float _heightRatio;



        // Use this for initialization
        void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _b = Mathf.Abs(FrontWheels[0].transform.localPosition.z);
            _c = Mathf.Abs(RearWheels[0].transform.localPosition.z);
            _heightRatio = 2.0f / (_b + _c);
        }

        // Update is called once per frame
        void Update()
        {
            foreach (var wheel in FrontWheels)
            {
                wheel.TargetAngle = (SteerAngle * Mathf.Rad2Deg) * 2;
            }
        }

        void FixedUpdate()
        {
            var allWheelsGrounded = FrontWheels.All(x => x.Grounded) && RearWheels.All(x => x.Grounded);
            if (!allWheelsGrounded)
                return;

            var vel3d = transform.InverseTransformVector(_rigidbody.velocity);
            _carVelocity = new Vector2(vel3d.z, vel3d.x);

            _speed = _carVelocity.magnitude;

            var avel = Mathf.Min(_speed, SteerSpeedBias);  // m/s
            Steer = Steer * (1.0f - (avel / SteerSpeedBias));
            SteerAngle = Steer * MaxSteer;

            if (Mathf.Abs(Throttle) < 0.1 && Mathf.Abs(_speed) < 0.5f)
            {
                _rigidbody.velocity = Vector3.zero;
                _carVelocity = Vector2.zero;
                _speed = 0;
                _rigidbody.angularVelocity = Vector3.zero;
            }
            if (_speed == 0)
                SteerAngle = 0;

            var rotAngle = 0.0f;
            var sideslip = 0.0f;
            if (Mathf.Abs(_speed) > 0.5f)
            {
                rotAngle = Mathf.Atan2(_rigidbody.angularVelocity.y, _carVelocity.x);
                sideslip = Mathf.Atan2(_carVelocity.y, _carVelocity.x);
            }

            var slipAngleFront = sideslip + rotAngle - SteerAngle;
            var slipAngleRear = sideslip - rotAngle;

            _totalWeight = _rigidbody.mass * Mathf.Abs(Physics.gravity.y);
            var weight = _totalWeight * 0.5f; // Weight per axle

            var weightDiff = _heightRatio * _rigidbody.mass * _carAcceleration.x; // --weight distribution between axles(stored to animate body)
            _weightFront = weight - weightDiff;
            _weightRear = weight + weightDiff;

            var slipFront = Mathf.Clamp(CorneringStiffnessFront * slipAngleFront, -MaxGrip, MaxGrip);
            var slipRear = Mathf.Clamp(CorneringStiffnessRear * slipAngleRear, -MaxGrip, MaxGrip);

            var fLateralFront = new Vector2(0, slipFront) * weight;
            var fLateralRear = new Vector2(0, slipRear) * weight;
            if (EBrake)
                fLateralRear *= 0.5f;

            _percentFront = _weightFront / weight - 1.0f;
            var weightShiftAngle = Mathf.Clamp(_percentFront * 50, -50, 50);
            var euler = Chassis.localRotation.eulerAngles;
            euler.x = weightShiftAngle;
            euler.z = Mathf.Clamp(((slipFront + slipRear) / (MaxGrip * 2)) * 5, -5, 5);
            Chassis.localRotation = Quaternion.Slerp(Chassis.localRotation, Quaternion.Euler(euler), Time.deltaTime * 5);

            _fTraction = Vector2.right * EngineForce * Throttle;

            if (_speed > 0 && Brake > 0)
            {
                _fTraction = -Vector2.right * BrakeConstant * Brake * Mathf.Sign(_carVelocity.x);
            }

            var fDrag = -DragConstant * _carVelocity * Mathf.Abs(_carVelocity.x);
            var fRollingResistance = -RollingResistanceConstant * _carVelocity;
            var fLong = _fTraction + fDrag + fRollingResistance;

            var forces = fLong + fLateralFront + fLateralRear;

            var torque = _b * fLateralFront.y - _c * fLateralRear.y;
            var angularAcceleration = torque / _rigidbody.mass; // Really inertia but...
            _rigidbody.angularVelocity += Vector3.up * angularAcceleration * Time.deltaTime;

            _carAcceleration = Time.deltaTime * forces / _rigidbody.mass;

            var worldAcceleration = transform.TransformVector(new Vector3(_carAcceleration.y, 0, _carAcceleration.x));
            _rigidbody.velocity += worldAcceleration;
        }

        //private void OnGUI()
        //{
        //    GUILayout.Label("Local velocity: " + _carVelocity + ", acceleration: " + _carAcceleration);
        //    GUILayout.Label("Speed: " + _speed);
        //    GUILayout.Label("Half weight: " + (_totalWeight * 0.5f) + ", Weight front: " + _weightFront + ", rear: " + _weightRear + ", percent front: " + _percentFront * 100 + "%");
        //    GUILayout.Label("Traction: " + _fTraction + ", max: " + _fTractionMax);
        //    GUILayout.Label("Wheel angvel: " + _wheelAngularVelocity + ", slip longitudal: " + _slipLongitudal);
        //}
    }
}