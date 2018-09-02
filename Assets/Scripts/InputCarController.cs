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
            if (Input.GetKeyDown(KeyCode.K))
            {
                _car.Kill();
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