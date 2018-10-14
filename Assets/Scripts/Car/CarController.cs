using System;
using Assets.Fileparsers;
using Assets.Scripts.Camera;
using Assets.Scripts.Car.Components;
using Assets.Scripts.Car.UI;
using Assets.Scripts.System;
using Assets.System;
using UnityEngine;

namespace Assets.Scripts.Car
{
    public enum DamageType
    {
        Projectile,
        Force
    }

    public class CarController : MonoBehaviour
    {
        private const int VehicleStartHealth = 1000;

        private Transform _transform;
        private Rigidbody _rigidBody;

        public WeaponsController WeaponsController;
        public SpecialsController SpecialsController;
        public SystemsPanel SystemsPanel;
        public GearPanel GearPanel;
        public RadarPanel RadarPanel;
        private CompassPanel _compassPanel;
        private CameraController _camera;
        private int _vehicleHealthGroups;
        private int _currentVehicleHealthGroup;
        private AudioSource _engineStartSound;
        private AudioSource _engineLoopSound;
        private bool _engineStarting;
        private float _engineStartTimer;
        private GameObject _gameObject;
        private CacheManager _cacheManager;
        private int[] _vehicleHitPoints;
        private int[] _vehicleStartHitPoints;

        public bool EngineRunning { get; private set; }
        public bool Arrived { get; set; }
        public Vdf Vdf { get; private set; }
        public Vcf Vcf { get; private set; }

        public bool Alive
        {
            get { return GetComponentHealth(SystemType.Vehicle) > 0; }
        }

        public bool Attacked { get; private set; }
        public int TeamId { get; set; }
        public CarMovement Movement { get; private set; }
        public CarAI AI { get; private set; }

        private int GetComponentHealth(SystemType healthType)
        {
            return _vehicleHitPoints[(int)healthType];
        }

        private int GetHealthGroup(SystemType system)
        {
            int systemValue = (int)system;
            int healthGroupCount;
            int startHealth;

            if (system == SystemType.Vehicle)
            {
                healthGroupCount = _vehicleHealthGroups;
                startHealth = _vehicleStartHitPoints[systemValue];
            }
            else
            {
                healthGroupCount = 5;
                startHealth = _vehicleStartHitPoints[systemValue];
            }
            
            if (_vehicleHitPoints[systemValue] <= 0)
            {
                return healthGroupCount - 1;
            }

            --healthGroupCount;
            int healthGroup = Mathf.CeilToInt(_vehicleHitPoints[systemValue] / (float)startHealth * healthGroupCount);
            return healthGroupCount - healthGroup;
        }

        private void SetComponentHealth(SystemType system, int value)
        {
            if (!Alive)
            {
                return;
            }

            _vehicleHitPoints[(int)system] = value;
            
            if (system == SystemType.Vehicle)
            {
                SetHealthGroup(GetHealthGroup(system));
                if (value <= 0)
                {
                    Explode();
                }
            }
            else
            {
                int newValue = _vehicleHitPoints[(int)system];
                if (newValue < 0)
                {
                    SetComponentHealth(SystemType.Vehicle, _vehicleHitPoints[(int)SystemType.Vehicle] + newValue);
                    // TODO: Randomly pick between nearby components to damage, not just vehicle's core health.
                    _vehicleHitPoints[(int)system] = 0;
                }
            }
        }

        public void ApplyDamage(DamageType damageType, Vector3 normal, int damageAmount)
        {
            float angle = Vector3.Angle(_transform.forward, normal);
            angle = Vector3.Angle(_transform.up, normal) > 90f ? 360f - angle : angle;

            SystemType system;
            switch (damageType)
            {
                case DamageType.Force:
                    if (angle < 90)
                    {
                        system = SystemType.FrontChassis;
                    }
                    else if (angle < 180)
                    {
                        system = SystemType.RightChassis;
                    }
                    else if (angle < 270)
                    {
                        system = SystemType.BackChassis;
                    }
                    else
                    {
                        system = SystemType.LeftChassis;
                    }
                    break;
                case DamageType.Projectile:
                    if (angle < 90)
                    {
                        system = SystemType.FrontArmor;
                    }
                    else if (angle < 180)
                    {
                        system = SystemType.RightArmor;
                    }
                    else if (angle < 270)
                    {
                        system = SystemType.BackArmor;
                    }
                    else
                    {
                        system = SystemType.LeftArmor;
                    }
                    break;
                default:
                    throw new NotSupportedException("Invalid damage type.");
            }

            int currentHealth = GetComponentHealth(system);
            SetComponentHealth(system, currentHealth - damageAmount);

            // Update UI
            if (SystemsPanel != null)
            {
                SystemsPanel.SetSystemHealthGroup(system, GetHealthGroup(system), true);
            }
        }

        public void ToggleEngine()
        {
            if (_engineStartSound == null || _engineStartSound.isPlaying)
            {
                return;
            }

            if (EngineRunning)
            {
                _engineLoopSound.Stop();
                EngineRunning = false;
            }
            else
            {
                _engineStartSound.Play();
                _engineStarting = true;
                _engineStartTimer = 0f;
            }
        }

        private void Awake()
        {
            _transform = transform;
            _gameObject = gameObject;
            _cacheManager = FindObjectOfType<CacheManager>();
            Movement = new CarMovement(this);
            AI = new CarAI(this);
            _camera = CameraManager.Instance.MainCamera.GetComponent<CameraController>();
            _rigidBody = GetComponent<Rigidbody>();
            EngineRunning = true;
            _currentVehicleHealthGroup = 1;
        }

        public void Configure(Vdf vdf, Vcf vcf)
        {
            Vdf = vdf;
            Vcf = vcf;

            _vehicleHealthGroups = Vdf.PartsThirdPerson.Count;

            _vehicleHitPoints = new int[9];
            _vehicleStartHitPoints = new int[9];

            _vehicleStartHitPoints[(int)SystemType.Vehicle] = VehicleStartHealth;
            _vehicleStartHitPoints[(int)SystemType.FrontArmor] = (int)vcf.ArmorFront;
            _vehicleStartHitPoints[(int)SystemType.RightArmor] = (int)vcf.ArmorRight;
            _vehicleStartHitPoints[(int)SystemType.BackArmor] = (int)vcf.ArmorRear;
            _vehicleStartHitPoints[(int)SystemType.LeftArmor] = (int)vcf.ArmorLeft;
            _vehicleStartHitPoints[(int)SystemType.FrontChassis] = (int)vcf.ChassisFront;
            _vehicleStartHitPoints[(int)SystemType.RightChassis] = (int)vcf.ChassisRight;
            _vehicleStartHitPoints[(int)SystemType.BackChassis] = (int)vcf.ChassisRear;
            _vehicleStartHitPoints[(int)SystemType.LeftChassis] = (int)vcf.ChassisLeft;

            for (int i = 0; i < 9; ++i)
            {
                _vehicleHitPoints[i] = _vehicleStartHitPoints[i];
            }
        }

        private void Start()
        {
            UpdateEngineSounds();
        }

        public void InitPanels()
        {
            Transform firstPersonTransform = _transform.Find("Chassis/FirstPerson");
            WeaponsController = new WeaponsController(this, Vcf, firstPersonTransform);
            SpecialsController = new SpecialsController(Vcf, firstPersonTransform);
            SystemsPanel = new SystemsPanel(firstPersonTransform);
            GearPanel = new GearPanel(firstPersonTransform);
            _compassPanel = new CompassPanel(firstPersonTransform);
            RadarPanel = new RadarPanel(this, firstPersonTransform);
        }

        private void Update()
        {
            if (!Alive)
            {
                return;
            }

            if (EngineRunning)
            {
                // Simple and temporary engine pitch adjustment code based on rigidbody velocity - should be using wheels.
                const float firstGearTopSpeed = 40f;
                const float gearRatioAdjustment = 1.5f;
                const float minPitch = 0.6f;
                const float maxPitch = 1.2f;

                float velocity = _rigidBody.velocity.magnitude;
                float gearMaxSpeed = firstGearTopSpeed;
                while (velocity / gearMaxSpeed > maxPitch - minPitch)
                {
                    gearMaxSpeed *= gearRatioAdjustment;
                }

                float enginePitch = minPitch + velocity / gearMaxSpeed;
                _engineLoopSound.pitch = enginePitch;
            }

            if (_engineStarting)
            {
                _engineStartTimer += Time.deltaTime;
                if (_engineStartTimer > _engineStartSound.clip.length - 0.5f)
                {
                    _engineLoopSound.Play();
                    EngineRunning = true;
                    _engineStarting = false;
                    _engineStartTimer = 0f;
                }
            }
            
            Movement.Update();

            if (_camera.FirstPerson)
            {
                if (_compassPanel != null)
                {
                    _compassPanel.UpdateCompassHeading(_transform.eulerAngles.y);
                }
            }
            
            // Always process radar panel, even outside first person view.
            if (RadarPanel != null)
            {
                RadarPanel.Update();
            }

            if (TeamId != 1 || !CameraManager.Instance.IsMainCameraActive)
            {
                AI.Navigate();
            }
        }

        private void FixedUpdate()
        {
            if (Movement != null)
            {
                Movement.FixedUpdate();
            }
        }

        private void SetHealthGroup(int healthGroupIndex)
        {
            _currentVehicleHealthGroup = healthGroupIndex;
            Transform parent = transform.Find("Chassis/ThirdPerson");
            for (int i = 0; i < _vehicleHealthGroups; ++i)
            {
                Transform child = parent.Find("Health " + i);
                child.gameObject.SetActive(healthGroupIndex == i);
            }
        }

        private void Explode()
        {
            _rigidBody.AddForce(Vector3.up * _rigidBody.mass * 5f, ForceMode.Impulse);

            InputCarController inputController = GetComponent<InputCarController>();
            if (inputController != null)
            {
                Destroy(inputController);
            }

            AudioSource explosionSource = _cacheManager.GetAudioSource(_gameObject, "xcar");
            if (explosionSource != null)
            {
                explosionSource.volume = 0.9f;
                explosionSource.Play();
            }

            EngineRunning = false;
            Destroy(_engineLoopSound);
            Destroy(_engineStartSound);

            Movement.Destroy();
            Movement = null;
            AI = null;

            WeaponsController = null;
            SpecialsController = null;
            SystemsPanel = null;
            GearPanel = null;
            _compassPanel = null;
            RadarPanel = null;

            Destroy(transform.Find("FrontLeft").gameObject);
            Destroy(transform.Find("FrontRight").gameObject);
            Destroy(transform.Find("BackLeft").gameObject);
            Destroy(transform.Find("BackRight").gameObject);
        }

        public void Kill()
        {
            SetComponentHealth(SystemType.Vehicle, 0);
        }

        public void Sit()
        {
            Movement.Brake = 1.0f;
            AI.Sit();
        }

        private void OnDrawGizmos()
        {
            if (AI != null)
            {
                AI.DrawGizmos();
            }
        }

        public void SetSpeed(int targetSpeed)
        {
            _rigidBody.velocity = _transform.forward * targetSpeed;
        }

        public void SetTargetPath(FSMPath path, int targetSpeed)
        {
            if (AI != null)
            {
                AI.SetTargetPath(path, targetSpeed);
            }
        }

        private void UpdateEngineSounds()
        {
            string engineStartSound;
            string engineLoopSound;

            switch (Vdf.VehicleSize)
            {
                case 1: // Small car
                    engineLoopSound = "eishp";
                    engineStartSound = "esshp";
                    engineStartSound += _currentVehicleHealthGroup;
                    break;
                case 2: // Medium car
                    engineLoopSound = "eihp";
                    engineStartSound = "eshp";
                    engineStartSound += _currentVehicleHealthGroup;
                    break;
                case 3: // Large car
                    engineLoopSound = "einp1";
                    engineStartSound = "esnp";
                    engineStartSound += _currentVehicleHealthGroup;
                    break;
                case 4: // Van
                    engineLoopSound = "eisv";
                    engineStartSound = "essv";
                    break;
                case 5: // Heavy vehicle
                    engineLoopSound = "eimarx";
                    engineStartSound = "esmarx";
                    break;
                case 6: // Tank
                    engineLoopSound = "eitank";
                    engineStartSound = "estank";
                    break;
                default:
                    Debug.LogWarningFormat("Unhandled vehicle size '{0}'. No vehicle sounds loaded.", Vdf.VehicleSize);
                    return;
            }

            engineStartSound += ".gpw";
            engineLoopSound += ".gpw";
            if (_engineStartSound == null || _engineStartSound.clip.name != engineStartSound)
            {
                if (_engineStartSound != null)
                {
                    Destroy(_engineStartSound);
                }

                _engineStartSound = _cacheManager.GetAudioSource(_gameObject, engineStartSound);
                if (_engineStartSound != null)
                {
                    _engineStartSound.volume = 0.6f;
                }
            }

            if (_engineLoopSound == null || _engineLoopSound.clip.name != engineLoopSound)
            {
                if (_engineLoopSound != null)
                {
                    Destroy(_engineLoopSound);
                }

                _engineLoopSound = _cacheManager.GetAudioSource(_gameObject, engineLoopSound);
                if (_engineLoopSound != null)
                {
                    _engineLoopSound.loop = true;
                    _engineLoopSound.volume = 0.6f;

                    if (EngineRunning)
                    {
                        _engineLoopSound.Play();
                    }
                }
            }
        }
    }
}