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
        private const int StartHealth = 100;

        private Transform _transform;
        private Rigidbody _rigidBody;

        public WeaponsController WeaponsController;
        public SpecialsController SpecialsController;
        public SystemsPanel SystemsPanel;
        public GearPanel GearPanel;
        public RadarPanel RadarPanel;
        private CompassPanel _compassPanel;
        private CameraController _camera;
        private int _healthGroups;
        private int _health;
        private int _currentHealthGroup;
        private AudioSource _engineStartSound;
        private AudioSource _engineLoopSound;
        private bool _engineStarting;
        private float _engineStartTimer;
        private GameObject _gameObject;
        private CacheManager _cacheManager;

        public bool EngineRunning { get; private set; }
        public bool Arrived { get; set; }
        public Vdf Vdf { get; set; }

        public bool Alive
        {
            get { return _health > 0; }
        }

        public bool Attacked { get; private set; }
        public int TeamId { get; set; }
        public CarMovement Movement { get; private set; }
        public CarAI AI { get; private set; }

        public int Health
        {
            get { return _health; }
            private set
            {
                if (_health <= 0)
                {
                    return;
                }

                _health = value;
                SetHealthGroup(_healthGroups - Mathf.FloorToInt((float) _health / (StartHealth + 1) * _healthGroups));

                if (_health <= 0)
                {
                    Explode();
                }
            }
        }

        public void ApplyDamage(DamageType damageType, Vector3 normal, int damageAmount)
        {
            float angle = Vector3.Angle(_transform.forward, normal);
            angle = (Vector3.Angle(_transform.up, normal) > 90f) ? 360f - angle : angle;

            // TODO: Apply damage to specific components.
            Health -= damageAmount;

            if (SystemsPanel != null)
            {
                // Update UI
                SystemsPanel.Systems system = SystemsPanel.GetSystemForDamage(damageType, angle);
                SystemsPanel.SetSystemHealthGroup(system, 1, true);
            }
        }

        public void ToggleEngine()
        {
            if (_engineStartSound.isPlaying)
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
            _health = StartHealth;
            _currentHealthGroup = 1;
            _gameObject = gameObject;
            _cacheManager = FindObjectOfType<CacheManager>();
            Movement = new CarMovement(this);
            AI = new CarAI(this);
            _camera = CameraManager.Instance.MainCamera.GetComponent<CameraController>();
            _rigidBody = GetComponent<Rigidbody>();
            EngineRunning = true;
        }

        private void Start()
        {
            UpdateEngineSounds();
            _healthGroups = Vdf.PartsThirdPerson.Count;
        }

        public void InitPanels(VcfParser.Vcf vcf)
        {
            Transform firstPersonTransform = _transform.Find("Chassis/FirstPerson");
            WeaponsController = new WeaponsController(this, vcf, firstPersonTransform);
            SpecialsController = new SpecialsController(vcf, firstPersonTransform);
            SystemsPanel = new SystemsPanel(firstPersonTransform);
            GearPanel = new GearPanel(firstPersonTransform);
            _compassPanel = new CompassPanel(firstPersonTransform);
            RadarPanel = new RadarPanel(this, firstPersonTransform);
        }

        private void Update()
        {
            if (_health <= 0)
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
            _currentHealthGroup = healthGroupIndex;
            Transform parent = transform.Find("Chassis/ThirdPerson");
            for (int i = 0; i < _healthGroups; ++i)
            {
                Transform child = parent.Find("Health " + i);
                child.gameObject.SetActive(healthGroupIndex == i + 1);
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
            Health = 0;
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
                    engineStartSound += _currentHealthGroup;
                    break;
                case 2: // Medium car
                    engineLoopSound = "eihp";
                    engineStartSound = "eshp";
                    engineStartSound += _currentHealthGroup;
                    break;
                case 3: // Large car
                    engineLoopSound = "einp1";
                    engineStartSound = "esnp";
                    engineStartSound += _currentHealthGroup;
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
                _engineStartSound.volume = 0.6f;
            }

            if (_engineLoopSound == null || _engineLoopSound.clip.name != engineLoopSound)
            {
                if (_engineLoopSound != null)
                {
                    Destroy(_engineLoopSound);
                }

                _engineLoopSound = _cacheManager.GetAudioSource(_gameObject, engineLoopSound);
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