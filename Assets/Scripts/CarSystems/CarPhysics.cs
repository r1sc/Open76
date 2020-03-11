using System;
using Assets.Scripts.Entities;
using Assets.Scripts.System;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Assets.Scripts.CarSystems
{
    public class CarPhysics : MonoBehaviour
    {
        private const float AirTimeLandingTreshold = 0.75f;
        private Rigidbody _rigidbody;

        public float Throttle;
        public float Brake;
        public bool EBrake;
        public float Steer;
        public float MaxSteer = 0.5f;
        public float SteerSpeedBias = 55f;

        public float EngineForce = 1000f;
        public float BrakeConstant = 1000f;
        public float DragConstant = 0f;
        public float RollingResistanceConstant = 15f;
        public float WheelRadius = 0.33f;
        public RaySusp[] FrontWheels, RearWheels;
        public Transform Chassis;
        public float SteerAngle;
        public float CorneringStiffnessFront = -5f;
        public float CorneringStiffnessRear = -5.2f;
        public float MaxGrip = 8f;

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
        private float _b;
        private float _c;
        private float _heightRatio;
        private float _airTime;

        private AudioSource _surfaceAudioSource;
        private AudioSource _landingAudioSource;
        private AudioClip _landingAudioClip1;
        private AudioClip _landingAudioClip2;
        private CacheManager _cacheManager;

        private bool _ready;

        private void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        public void Initialise(Transform chassisTransform, RaySusp[] frontWheels, RaySusp[] rearWheels)
        {
            Chassis = chassisTransform;
            FrontWheels = frontWheels;
            RearWheels = rearWheels;

            _b = Mathf.Abs(FrontWheels[0].transform.localPosition.z);
            _c = Mathf.Abs(RearWheels[0].transform.localPosition.z);
            _heightRatio = 2.0f / (_b + _c);

            _cacheManager = CacheManager.Instance;
            _landingAudioSource = gameObject.AddComponent<AudioSource>();
            _landingAudioSource.playOnAwake = false;
            _landingAudioSource.volume = 0.8f;
            _landingAudioSource.spatialBlend = 1.0f;
            _landingAudioSource.maxDistance = 75f;
            _landingAudioSource.minDistance = 7.5f;
            _landingAudioClip1 = _cacheManager.GetAudioClip("vland");
            _landingAudioClip2 = _cacheManager.GetAudioClip("vlanding");

            _ready = true;
        }

        public void Destroy()
        {
            if (_surfaceAudioSource != null)
            {
                Object.Destroy(_surfaceAudioSource);
                _surfaceAudioSource = null;
            }

            if (_landingAudioSource != null)
            {
                Object.Destroy(_landingAudioSource);
                _landingAudioSource = null;
            }
        }

        // Update is called once per frame
        public void Update()
        {
            if (!_ready)
            {
                return;
            }

            foreach (RaySusp wheel in FrontWheels)
            {
                wheel.TargetAngle = (SteerAngle * Mathf.Rad2Deg) * 2;
            }
        }

        private void UpdateSurfaceSound()
        {
            Ray terrainRay = new Ray(transform.position, Vector3.down);
            if (Physics.Raycast(terrainRay, out RaycastHit hitInfo))
            {
                string objectTag = hitInfo.collider.gameObject.tag;
                string surfaceSoundName;
                switch (objectTag)
                {
                    case "road":
                        surfaceSoundName = "vcdgrav.gpw";
                        break;
                    default:
                        surfaceSoundName = "vcddirt.gpw";
                        break;
                }

                if (_surfaceAudioSource == null || _surfaceAudioSource.clip == null || _surfaceAudioSource.clip.name != surfaceSoundName)
                {
                    if (_surfaceAudioSource != null)
                    {
                        Object.Destroy(_surfaceAudioSource);
                    }
                    
                    _surfaceAudioSource = _cacheManager.GetAudioSource(gameObject, surfaceSoundName);
                    _surfaceAudioSource.loop = true;
                    _surfaceAudioSource.volume = 0.6f;
                    _surfaceAudioSource.Play();
                }
            }

            if (_surfaceAudioSource != null)
            {
                _surfaceAudioSource.volume = Mathf.Min(_rigidbody.velocity.magnitude * 0.025f, 0.6f);
                if (!_surfaceAudioSource.isPlaying)
                {
                    _surfaceAudioSource.Play();
                }
            }
        }

        public void FixedUpdate()
        {
            if (!_ready)
            {
                return;
            }
			
			int groundedWheels = 0;
            for (int i = 0; i < FrontWheels.Length; ++i)
            {
                if (FrontWheels[i].Grounded) ++groundedWheels;
            }
            for (int i = 0; i < RearWheels.Length; ++i)
            {
                if (RearWheels[i].Grounded) ++groundedWheels;
            }

            if (groundedWheels == 0)
            {
                _airTime += Time.deltaTime;
                if (_surfaceAudioSource != null && _surfaceAudioSource.isPlaying)
                {
                    _surfaceAudioSource.Stop();
                }
            }

            bool allWheelsGrounded = groundedWheels == FrontWheels.Length + RearWheels.Length;

            if (!allWheelsGrounded)
            {
                return;
            }

            if (_airTime > AirTimeLandingTreshold && !_landingAudioSource.isPlaying)
            {
                _landingAudioSource.clip = Random.Range(0, 2) == 0 ? _landingAudioClip1 : _landingAudioClip2;
                _landingAudioSource.Play();
            }

            _airTime = 0.0f;
            UpdateSurfaceSound();
            Vector3 vel3d = transform.InverseTransformVector(_rigidbody.velocity);

            _carVelocity = new Vector2(vel3d.z, vel3d.x);

            _speed = _carVelocity.magnitude;

            float avel = Mathf.Min(_speed, SteerSpeedBias);  // m/s
            Steer = Steer * (1.0f - (avel / SteerSpeedBias));
            SteerAngle = Steer * MaxSteer;

            if (Mathf.Abs(Throttle) < 0.1 && Mathf.Abs(_speed) < 0.5f)
            {
                _rigidbody.velocity = Vector3.zero;
                _carVelocity = Vector2.zero;
                _speed = 0;
                _rigidbody.angularVelocity = Vector3.zero;
            }
            if (Math.Abs(_speed) < float.Epsilon)
                SteerAngle = 0;

            float rotAngle = 0.0f;
            float sideslip = 0.0f;
            if (Mathf.Abs(_speed) > 0.5f)
            {
                rotAngle = Mathf.Atan2(_rigidbody.angularVelocity.y, _carVelocity.x);
                sideslip = Mathf.Atan2(_carVelocity.y, _carVelocity.x);
            }

            float slipAngleFront = sideslip + rotAngle - SteerAngle;
            float slipAngleRear = sideslip - rotAngle;

            _totalWeight = _rigidbody.mass * Mathf.Abs(Physics.gravity.y);
            float weight = _totalWeight * 0.5f; // Weight per axle

            float weightDiff = _heightRatio * _rigidbody.mass * _carAcceleration.x; // --weight distribution between axles(stored to animate body)
            _weightFront = weight - weightDiff;
            _weightRear = weight + weightDiff;

            float slipFront = Mathf.Clamp(CorneringStiffnessFront * slipAngleFront, -MaxGrip, MaxGrip);
            float slipRear = Mathf.Clamp(CorneringStiffnessRear * slipAngleRear, -MaxGrip, MaxGrip);

            Vector2 fLateralFront = new Vector2(0, slipFront) * weight;
            Vector2 fLateralRear = new Vector2(0, slipRear) * weight;
            if (EBrake)
                fLateralRear *= 0.5f;

            _percentFront = _weightFront / weight - 1.0f;
            float weightShiftAngle = Mathf.Clamp(_percentFront * 50, -50, 50);
            Vector3 euler = Chassis.localRotation.eulerAngles;
            euler.x = weightShiftAngle;
            euler.z = Mathf.Clamp(((slipFront + slipRear) / (MaxGrip * 2)) * 5, -5, 5);
            Chassis.localRotation = Quaternion.Slerp(Chassis.localRotation, Quaternion.Euler(euler), Time.deltaTime * 5);

            _fTraction = Vector2.right * EngineForce * Throttle;

            if (_speed > 0 && Brake > 0)
            {
                _fTraction = -Vector2.right * BrakeConstant * Brake * Mathf.Sign(_carVelocity.x);
            }

            Vector2 fDrag = -DragConstant * _carVelocity * Mathf.Abs(_carVelocity.x);
            Vector2 fRollingResistance = -RollingResistanceConstant * _carVelocity;
            Vector2 fLong = _fTraction + fDrag + fRollingResistance;

            Vector2 forces = fLong + fLateralFront + fLateralRear;

            float torque = _b * fLateralFront.y - _c * fLateralRear.y;
            float angularAcceleration = torque / _rigidbody.mass; // Really inertia but...
            _rigidbody.angularVelocity += Vector3.up * angularAcceleration * Time.deltaTime;

            _carAcceleration = Time.deltaTime * forces / _rigidbody.mass;

            Vector3 worldAcceleration = transform.TransformVector(new Vector3(_carAcceleration.y, 0, _carAcceleration.x));
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