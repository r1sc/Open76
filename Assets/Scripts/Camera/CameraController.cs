using Assets.Scripts.Camera;
using Assets.Scripts.CarSystems;
using UnityEngine;
using UnityEngine.XR;

namespace Assets
{
    [RequireComponent(typeof(SmoothFollow))]
    public class CameraController : MonoBehaviour
    {
        private SmoothFollow _smoothFollow;
        private Car _player;

        public bool FirstPerson { get; private set; }

        private enum ChassisView
        {
            FirstPerson,
            ThirdPerson,
            AllHidden
        }

        // Use this for initialization
        void Start()
        {
            _smoothFollow = GetComponent<SmoothFollow>();
        }

        // Update is called once per frame
        void Update()
        {
            if (!CameraManager.Instance.IsMainCameraActive)
            {
                return;
            }

            if (_player == null)
            {
                Transform target = _smoothFollow.Target;
                if (target != null)
                {
                    _player = target.GetComponent<Car>();
                }
            }
            else
            {
                if (!_player.Alive)
                {
                    SetCameraThirdPerson();
                    FirstPerson = false;
                    return;
                }
            }
            
            if (Input.GetKeyDown(KeyCode.F1))
            {
                SetCameraFirstPersonAtVLOCIndex(0);
                SetVisibleChassisModel(ChassisView.FirstPerson);
                FirstPerson = true;
            }
            else if (Input.GetKeyDown(KeyCode.F2))
            {
                SetCameraThirdPerson();
                FirstPerson = false;
            }
            else if (Input.GetKeyDown(KeyCode.F3))
            {
                SetCameraFirstPersonAtVLOCIndex(1);
                SetVisibleChassisModel(ChassisView.AllHidden);
                FirstPerson = false;
            }
            else if (Input.GetKeyDown(KeyCode.F4))
            {
                SetCameraAtWheelIndex(0);
                SetVisibleChassisModel(ChassisView.ThirdPerson);
                FirstPerson = false;
            }
            else if (Input.GetKeyDown(KeyCode.F5))
            {
                SetCameraAtWheelIndex(1);
                SetVisibleChassisModel(ChassisView.ThirdPerson);
                FirstPerson = false;
            }
            else if (Input.GetKeyDown(KeyCode.F6))
            {
                SetCameraAtWheelIndex(2);
                SetVisibleChassisModel(ChassisView.ThirdPerson);
                FirstPerson = false;
            }
            else if (Input.GetKeyDown(KeyCode.F7))
            {
                SetCameraAtWheelIndex(3);
                SetVisibleChassisModel(ChassisView.ThirdPerson);
                FirstPerson = false;
            }

            if(Input.GetKeyDown(KeyCode.R))
            {
                InputTracking.Recenter();
            }

            if (FirstPerson)
            {
                var targetRotation = Quaternion.Euler(-14, 0, 0);
                if (Input.GetKey(KeyCode.Keypad6))
                    targetRotation = Quaternion.Euler(-14, 90, 0);
                else if (Input.GetKey(KeyCode.Keypad2))
                    targetRotation = Quaternion.Euler(-14, 180, 0);
                else if (Input.GetKey(KeyCode.Keypad4))
                    targetRotation = Quaternion.Euler(-14, -90, 0);
                if (Input.GetKey(KeyCode.Keypad8))
                    targetRotation = Quaternion.Euler(7, 0, 0);

                transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * 6);
            }
        }

        private void SetCameraAtWheelIndex(int wheelIndex)
        {
            var suspensions = _player.GetComponentsInChildren<RaySusp>();
            if(wheelIndex < suspensions.Length)
            {
                var wheel = suspensions[wheelIndex].transform;
                var target = wheel.Find("Mesh").GetChild(0);

                transform.parent = wheel;
                transform.localPosition = target.localPosition;
                transform.localRotation = Quaternion.Euler(-14, 0, 0);
                _smoothFollow.enabled = false;
            }
        }

        private void SetCameraThirdPerson()
        {
            _smoothFollow.Target = _player.transform;
            _smoothFollow.enabled = true;
            transform.parent = null;

            SetVisibleChassisModel(ChassisView.ThirdPerson);
        }

        private void SetCameraFirstPersonAtVLOCIndex(int vlocIndex)
        {
            int i = 0;
            Transform vloc = null;
            foreach (Transform child in _player.transform)
            {
                if (child.name == "VLOC")
                {
                    if (i == vlocIndex)
                    {
                        vloc = child;
                    }
                    i++;
                }
            }

            if (vloc == null)
            {
                Debug.LogWarning("Cannot find VLOC with index " + vlocIndex);
                return;
            }

            transform.parent = vloc;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Euler(-14, 0, 0);
            _smoothFollow.enabled = false;
        }

        private void SetVisibleChassisModel(ChassisView chassisView)
        {
            var thirdPerson = _player.transform.Find("Chassis/ThirdPerson");
            thirdPerson.gameObject.SetActive(chassisView == ChassisView.ThirdPerson);

            var firstPerson = _player.transform.Find("Chassis/FirstPerson");
            firstPerson.gameObject.SetActive(chassisView == ChassisView.FirstPerson);

            var suspensions = _player.GetComponentsInChildren<RaySusp>();
            foreach (var suspension in suspensions)
            {
                suspension.SetWheelVisibile(chassisView == ChassisView.ThirdPerson);
            }            
        }

        public void SetCameraPositionAndLookAt(Vector3 position, Vector3 lookat)
        {
            //var world = GameObject.Find("World");
            //var worldPos = world.transform.position;
            //position += worldPos;
            //lookat += worldPos;

            transform.position = position;
            transform.LookAt(lookat);
        }
    }
}