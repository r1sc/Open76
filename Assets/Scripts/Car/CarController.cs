using Assets.Fileparsers;
using Assets.Scripts.Camera;
using Assets.Scripts.Car.Ui;
using Assets.Scripts.System;
using UnityEngine;

namespace Assets.Scripts.Car
{
    public class CarController : MonoBehaviour
    {
        private const int StartHealth = 100;

        private Transform _transform;
        private Rigidbody _rigidBody;
        private WeaponsPanel _weaponsPanel;
        private SystemsPanel _systemsPanel;
        private int _healthGroups;
        private int _health;

        public bool Arrived { get; set; }

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

        private void Awake()
        {
            _transform = transform;
            _health = StartHealth;
            Movement = new CarMovement(this);
            AI = new CarAI(this);
            _rigidBody = GetComponent<Rigidbody>();

            if (_transform.childCount > 0)
            {
                _healthGroups = _transform.Find("Chassis/ThirdPerson").childCount;
            }
        }

        public void InitPanels(VcfParser.Vcf vcf)
        {
            Transform firstPersonTransform = _transform.Find("Chassis/FirstPerson");
            _weaponsPanel = new WeaponsPanel(vcf, firstPersonTransform);
            _systemsPanel = new SystemsPanel(firstPersonTransform);
        }

        private void Update()
        {
            if (_health <= 0)
            {
                return;
            }

            Movement.Update();

            if (TeamId != 1 || !CameraManager.Instance.IsMainCameraActive)
            {
                AI.Navigate();
            }
        }

        private void FixedUpdate()
        {
            Movement.FixedUpdate();
        }

        private void SetHealthGroup(int healthGroupIndex)
        {
            Transform parent = transform.Find("Chassis/ThirdPerson");
            for (int i = 0; i < _healthGroups; ++i)
            {
                Transform child = parent.GetChild(i);
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

            Movement = null;
            AI = null;

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
    }
}