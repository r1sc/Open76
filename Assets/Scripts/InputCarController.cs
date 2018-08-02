using Assets.Car;
using Assets.Scripts.Camera;
using UnityEngine;

namespace Assets
{
    class InputCarController : MonoBehaviour
    {
        private NewCar _car;

        void Start()
        {
            _car = GetComponent<NewCar>();
        }

        void Update()
        {
            if (!CameraManager.Instance.IsMainCameraActive || !_carAi.Alive)
            {
                return;
            }

            var throttle = Input.GetAxis("Vertical");
            var brake = -Mathf.Min(0, throttle);
            throttle = Mathf.Max(0, throttle);

            _car.Throttle = throttle;
            _car.Brake = brake;

            var steering = Input.GetAxis("Horizontal");
            _car.Steer = steering;

            _car.EBrake = Input.GetButton("E-brake");
            // _car.RearSlip = ebrake;

        }
    }

}