using System;
using Assets.Fileparsers;
using Assets.Scripts.Camera;
using Assets.Scripts.CarSystems.Components;
using Assets.Scripts.CarSystems.UI;
using Assets.Scripts.Entities;
using Assets.Scripts.System;
using Assets.System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts.CarSystems
{
    public enum DamageType
    {
        Projectile,
        Force
    }

    public class Car : WorldEntity
    {
        public static bool FireWeapons;

        private const int VehicleStartHealth = 550; // TODO: Figure out where this is stored?
        private const int CoreStartHealth = 250; // TODO: Figure out where this is stored?
        private const int TireStartHealth = 100; // TODO: Parse.

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

        public override bool Alive
        {
            get { return GetComponentHealth(SystemType.Vehicle) > 0; }
        }

        public bool Attacked { get; private set; }
        public int TeamId { get; set; }
        public bool IsPlayer { get; set; }
        public int Skill1 { get; set; }
        public int Skill2 { get; set; }
        public int Aggressiveness { get; set; }
        public CarPhysics Movement { get; private set; }
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
                    SystemType coreComponent = GetCoreComponent();
                    SetComponentHealth(coreComponent, _vehicleHitPoints[(int)coreComponent] + newValue);
                    _vehicleHitPoints[(int)system] = 0;
                }
            }

            // Update UI
            if (SystemsPanel != null && system != SystemType.Vehicle)
            {
                SystemsPanel.SetSystemHealthGroup(system, GetHealthGroup(system), true);
            }
        }

        private SystemType GetCoreComponent()
        {
            SystemType system;

            SystemType[] coreSystems =
            {
                SystemType.Vehicle,
                SystemType.Brakes,
                SystemType.Engine,
                SystemType.Suspension
            };

            int iterations = 0;
            const int maxIterations = 20;

            do
            {
                int coreIndex = Random.Range(0, coreSystems.Length);
                system = coreSystems[coreIndex];
                ++iterations;
            } while (GetComponentHealth(system) == 0 && iterations < maxIterations);

            if (iterations == maxIterations)
            {
                return SystemType.Vehicle;
            }

            return system;
        }

        public void ApplyDamage(DamageType damageType, Vector3 normal, int damageAmount)
        {
            float angle = Quaternion.FromToRotation(Vector3.up, normal).eulerAngles.z;

            // TODO: Figure out how tire damage should be applied here.

            SystemType system;
            switch (damageType)
            {
                case DamageType.Force:
                    if (angle >= 45f && angle < 135f)
                    {
                        system = SystemType.RightChassis;
                    }
                    else if (angle >= 135f && angle < 225f)
                    {
                        system = SystemType.BackChassis;
                    }
                    else if (angle >= 225f && angle < 315f)
                    {
                        system = SystemType.LeftChassis;
                    }
                    else
                    {
                        system = SystemType.FrontChassis;
                    }
                    break;
                case DamageType.Projectile:
                    if (angle >= 45f && angle < 135f)
                    {
                        system = SystemType.RightArmor;
                    }
                    else if (angle >= 135f && angle < 225f)
                    {
                        system = SystemType.BackArmor;
                    }
                    else if (angle >= 225f && angle < 315f)
                    {
                        system = SystemType.LeftArmor;
                    }
                    else
                    {
                        system = SystemType.FrontArmor;
                    }
                    break;
                default:
                    throw new NotSupportedException("Invalid damage type.");
            }

            int currentHealth = GetComponentHealth(system);
            SetComponentHealth(system, currentHealth - damageAmount);
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
            _cacheManager = CacheManager.Instance;
            Movement = new CarPhysics(this);
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

            int systemCount = (int) SystemType.TotalSystems;
            _vehicleHitPoints = new int[systemCount];
            _vehicleStartHitPoints = new int[systemCount];

            _vehicleStartHitPoints[(int)SystemType.Vehicle] = VehicleStartHealth;
            _vehicleStartHitPoints[(int)SystemType.Suspension] = CoreStartHealth;
            _vehicleStartHitPoints[(int)SystemType.Brakes] = CoreStartHealth;
            _vehicleStartHitPoints[(int)SystemType.Engine] = CoreStartHealth;

            _vehicleStartHitPoints[(int)SystemType.FrontArmor] = (int)vcf.ArmorFront;
            _vehicleStartHitPoints[(int)SystemType.RightArmor] = (int)vcf.ArmorRight;
            _vehicleStartHitPoints[(int)SystemType.BackArmor] = (int)vcf.ArmorRear;
            _vehicleStartHitPoints[(int)SystemType.LeftArmor] = (int)vcf.ArmorLeft;

            _vehicleStartHitPoints[(int)SystemType.FrontChassis] = (int)vcf.ChassisFront;
            _vehicleStartHitPoints[(int)SystemType.RightChassis] = (int)vcf.ChassisRight;
            _vehicleStartHitPoints[(int)SystemType.BackChassis] = (int)vcf.ChassisRear;
            _vehicleStartHitPoints[(int)SystemType.LeftChassis] = (int)vcf.ChassisLeft;

            _vehicleStartHitPoints[(int)SystemType.TireFL] = TireStartHealth;
            _vehicleStartHitPoints[(int)SystemType.TireFR] = TireStartHealth;
            _vehicleStartHitPoints[(int)SystemType.TireBL] = TireStartHealth;
            _vehicleStartHitPoints[(int)SystemType.TireBR] = TireStartHealth;

            for (int i = 0; i < systemCount; ++i)
            {
                _vehicleHitPoints[i] = _vehicleStartHitPoints[i];
            }
        }

        private void Start()
        {
            EntityManager.Instance.RegisterCar(this);

            UpdateEngineSounds();

            Transform firstPersonTransform = _transform.Find("Chassis/FirstPerson");
            WeaponsController = new WeaponsController(this, Vcf, firstPersonTransform);
            SpecialsController = new SpecialsController(Vcf, firstPersonTransform);
        }

        public void InitPanels()
        {
            Transform firstPersonTransform = _transform.Find("Chassis/FirstPerson");
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

            if (!IsPlayer || !CameraManager.Instance.IsMainCameraActive)
            {
                AI.Navigate();
            }

            if (!IsPlayer && FireWeapons)
            {
                if (WeaponsController != null)
                {
                    WeaponsController.Fire(0);
                }
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

            CarInput carInputController = GetComponent<CarInput>();
            if (carInputController != null)
            {
                Destroy(carInputController);
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

        private void OnDestroy()
        {
            EntityManager.Instance.UnregisterCar(this);
        }

        public void SetSpeed(int targetSpeed)
        {
            _rigidBody.velocity = _transform.forward * targetSpeed;
        }

        public bool AtFollowTarget()
        {
            if (AI != null)
            {
                return AI.AtFollowTarget();
            }

            return false;
        }

        public void SetFollowTarget(Car targetCar, int xOffset, int targetSpeed)
        {
            if (AI != null)
            {
                AI.SetFollowTarget(targetCar, xOffset, targetSpeed);
            }
        }

        public void SetTargetPath(FSMPath path, int targetSpeed)
        {
            if (AI != null)
            {
                AI.SetTargetPath(path, targetSpeed);
            }
        }

        public bool IsWithinNav(FSMPath path, int distance)
        {
            if (AI != null)
            {
                return AI.IsWithinNav(path, distance);
            }

            return false;
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