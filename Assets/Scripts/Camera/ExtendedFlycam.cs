using UnityEngine;

namespace Assets.Scripts.Camera
{
    public class ExtendedFlycam : MonoBehaviour
    {

        /*
        EXTENDED FLYCAM
            Desi Quintans (CowfaceGames.com), 17 August 2012.
            Based on FlyThrough.js by Slin (http://wiki.unity3d.com/index.php/FlyThrough), 17 May 2011.

        LICENSE
            Free as in speech, and free as in beer.

        FEATURES
            WASD/Arrows:    Movement
                      Q:    Climb
                      E:    Drop
                          Shift:    Move faster
                        Control:    Move slower
                            End:    Toggle cursor locking to screen (you can also press Ctrl+P to toggle play mode on and off).
        */

        public float CameraSensitivity = 90;
        public float ClimbSpeed = 4;
        public float NormalMoveSpeed = 10;
        public float SlowMoveFactor = 0.25f;
        public float FastMoveFactor = 3;

        private float _rotationX = 0.0f;
        private float _rotationY = 0.0f;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            _rotationX += Input.GetAxis("Mouse X") * CameraSensitivity * Time.deltaTime;
            _rotationY += Input.GetAxis("Mouse Y") * CameraSensitivity * Time.deltaTime;
            _rotationY = Mathf.Clamp(_rotationY, -90, 90);

            transform.localRotation = Quaternion.AngleAxis(_rotationX, Vector3.up);
            transform.localRotation *= Quaternion.AngleAxis(_rotationY, Vector3.left);

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                transform.position += transform.forward * (NormalMoveSpeed * FastMoveFactor) * Input.GetAxis("Vertical") * Time.deltaTime;
                transform.position += transform.right * (NormalMoveSpeed * FastMoveFactor) * Input.GetAxis("Horizontal") * Time.deltaTime;
            }
            else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                transform.position += transform.forward * (NormalMoveSpeed * SlowMoveFactor) * Input.GetAxis("Vertical") * Time.deltaTime;
                transform.position += transform.right * (NormalMoveSpeed * SlowMoveFactor) * Input.GetAxis("Horizontal") * Time.deltaTime;
            }
            else
            {
                transform.position += transform.forward * NormalMoveSpeed * Input.GetAxis("Vertical") * Time.deltaTime;
                transform.position += transform.right * NormalMoveSpeed * Input.GetAxis("Horizontal") * Time.deltaTime;
            }


            if (Input.GetKey(KeyCode.Q))
            { transform.position += transform.up * ClimbSpeed * Time.deltaTime; }
            if (Input.GetKey(KeyCode.E))
            { transform.position -= transform.up * ClimbSpeed * Time.deltaTime; }

            if (Input.GetKeyDown(KeyCode.End))
            {
                Cursor.lockState = (Cursor.lockState == CursorLockMode.None) ? CursorLockMode.Locked : CursorLockMode.None;
            }
        }
    }
}