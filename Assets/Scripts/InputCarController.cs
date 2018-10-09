using Assets.Scripts.Camera;
using Assets.Scripts.Car;
using UnityEngine;

namespace Assets
{
    class InputCarController : MonoBehaviour
    {
        private CarController _car;
        private CarMovement _movement;

        void Start()
        {
            _car = GetComponent<CarController>();
            _movement = _car.Movement;
        }

        void Update()
        {
            // Kill player.
            if (Input.GetKeyDown(KeyCode.K))
            {
                _car.Kill();
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

            if (!CameraManager.Instance.IsMainCameraActive || !_car.Alive)
            {
                return;
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