using Assets.Car;
using Assets.Scripts.Camera;
using UnityEngine;

namespace Assets
{
    class InputCarController : MonoBehaviour
    {
        private NewCar _car;
        private CarAI _carAi;

        void Start()
        {
            _car = GetComponent<NewCar>();
            _carAi = GetComponent<CarAI>();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.X))
            {
                CarAI car = GetComponent<CarAI>();
                car.Kill();
            }

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