using Assets.Scripts.Camera;
using Assets.Scripts.CarSystems;
using UnityEngine;

namespace Assets
{
    class CarInput : MonoBehaviour
    {
        private Car _car;
        private CarPhysics _movement;

        void Start()
        {
            _car = GetComponent<Car>();
            _movement = _car.Movement;
        }

        void Update()
        {
            if (!CameraManager.Instance.IsMainCameraActive || !_car.Alive)
            {
                return;
            }
            
            // Kill player.
            if (Input.GetKeyDown(KeyCode.K))
            {
                _car.Kill();
            }

            // Debug: Toggle all AI cars to fire.
            if (Input.GetKeyDown(KeyCode.Z))
            {
                Car.FireWeapons = !Car.FireWeapons;
            }

            // Cycle radar target.
            if (Input.GetKeyDown(KeyCode.E))
            {
                _car.RadarPanel.CycleTarget();
            }

            // Toggle radar range.
            if (Input.GetKeyDown(KeyCode.R))
            {
                _car.RadarPanel.ToggleRange();
            }

            // Target nearest enemy.
            if (Input.GetKeyDown(KeyCode.T))
            {
                _car.RadarPanel.TargetNearest();
            }

            // Clear radar target.
            if (Input.GetKeyDown(KeyCode.Y))
            {
                _car.RadarPanel.ClearTarget();
            }

            // Cycle weapon.
            if (Input.GetKeyDown(KeyCode.Return))
            {
                _car.WeaponsController.CycleWeapon();
            }

            // Fire active weapon(s).
            if (Input.GetKey(KeyCode.Space))
            {
                _car.WeaponsController.Fire(-1);
            }

            // Fire weapon 1.
            if (Input.GetKey(KeyCode.Alpha1))
            {
                _car.WeaponsController.Fire(0);
            }

            // Fire weapon 2.
            if (Input.GetKey(KeyCode.Alpha2))
            {
                _car.WeaponsController.Fire(1);
            }

            // Fire weapon 3.
            if (Input.GetKey(KeyCode.Alpha3))
            {
                _car.WeaponsController.Fire(2);
            }

            // Fire weapon 4.
            if (Input.GetKey(KeyCode.Alpha4))
            {
                _car.WeaponsController.Fire(3);
            }

            // Fire weapon 5.
            if (Input.GetKey(KeyCode.Alpha5))
            {
                _car.WeaponsController.Fire(4);
            }

            // Fire special 1.
            if (Input.GetKey(KeyCode.Alpha6))
            {
                _car.SpecialsController.Fire(0);
            }

            // Fire special 2.
            if (Input.GetKey(KeyCode.Alpha7))
            {
                _car.SpecialsController.Fire(1);
            }

            // Fire special 3.
            if (Input.GetKey(KeyCode.Alpha8))
            {
                _car.SpecialsController.Fire(2);
            }

            // Start / Stop engine.
            if (Input.GetKeyDown(KeyCode.S))
            {
                _car.ToggleEngine();
            }

            var throttle = Input.GetAxis("Vertical");
            var brake = -Mathf.Min(0, throttle);
            throttle = Mathf.Max(0, throttle);

            if (!_car.EngineRunning)
            {
                throttle = 0f;
            }

            _movement.Throttle = throttle;
            _movement.Brake = brake;

            var steering = Input.GetAxis("Horizontal");
            _movement.Steer = steering;

            _movement.EBrake = Input.GetButton("E-brake");
            // _car.RearSlip = ebrake;

        }
    }

}