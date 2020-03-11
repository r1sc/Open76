using Assets.Scripts.Camera;
using Assets.Scripts.Entities;
using UnityEngine;

namespace Assets.Scripts.CarSystems
{
    internal class CarInput : MonoBehaviour
    {
        private Car _car;
        private CarPhysics _carPhysics;

        private void Start()
        {
            _car = GetComponent<Car>();
            _carPhysics = GetComponent<CarPhysics>();
        }

        private void Update()
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
            else if (Input.GetKey(KeyCode.Alpha1)) // Fire weapon 1.
            {
                _car.WeaponsController.Fire(0);
            }
            else if (Input.GetKey(KeyCode.Alpha2)) // Fire weapon 2.
            {
                _car.WeaponsController.Fire(1);
            }
            else if (Input.GetKey(KeyCode.Alpha3)) // Fire weapon 3.
            {
                _car.WeaponsController.Fire(2);
            }
            else if (Input.GetKey(KeyCode.Alpha4)) // Fire weapon 4.
            {
                _car.WeaponsController.Fire(3);
            }
            else if (Input.GetKey(KeyCode.Alpha5)) // Fire weapon 5.
            {
                _car.WeaponsController.Fire(4);
            }
            else if (Input.GetKey(KeyCode.Alpha6)) // Fire special 1.
            {
                _car.SpecialsController.Fire(0);
            }
            else
            {
                _car.WeaponsController.StopFiring();
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

            float throttle = Input.GetAxis("Vertical");
            float brake = -Mathf.Min(0, throttle);
            throttle = Mathf.Max(0, throttle);

            if (!_car.EngineRunning)
            {
                throttle = 0f;
            }

            _carPhysics.Throttle = throttle;
            _carPhysics.Brake = brake;

            float steering = Input.GetAxis("Horizontal");
            _carPhysics.Steer = steering;

            _carPhysics.EBrake = Input.GetButton("E-brake");
            // _car.RearSlip = ebrake;

        }
    }

}