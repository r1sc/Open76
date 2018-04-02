using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Car
{
    public class RaySusp : MonoBehaviour
    {
        public float SpringLength;
        public bool Grounded;
        public float SpringConstant;
        public float Damping;
        public float WheelRadius;
        private float _lastSpringLength;
        private Rigidbody _rigidbody;
        private Transform _wheelGraphic;

        public float TargetAngle;

        // Use this for initialization
        void Start()
        {
            _rigidbody = GetComponentInParent<Rigidbody>();
            _wheelGraphic = transform.Find("Mesh");
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            SpringPhysics();

            var euler = transform.localRotation.eulerAngles;
            euler.y = TargetAngle; // Mathf.Lerp(euler.y, TargetAngle, Time.deltaTime * 2);
            transform.localRotation = Quaternion.Euler(euler);
        }


        private void SpringPhysics()
        {
            RaycastHit rayHit;
            var springNow = SpringLength;
            if (Physics.Raycast(transform.position, -transform.up, out rayHit, SpringLength))
            {
                springNow = rayHit.distance; // * Random.Range(0.7f, 1.0f);  //for rough terrain
                Grounded = true;
            }
            else
            {
                Grounded = false;
            }

            // Mathf.Max(0, springNow * (1.0f - Pressure));

            var displacement = SpringLength - springNow;
            var force = transform.up * SpringConstant * displacement;

            var springVel = springNow - _lastSpringLength;
            var wheelVel = springVel * transform.up;
            //Debug.DrawLine(transform.position, transform.position + wheelVel * 10, Color.yellow);
            var damper = -Damping * wheelVel;
            force += damper;

            _rigidbody.AddForceAtPosition(force, transform.position, ForceMode.Force);
            //Debug.DrawLine(transform.position, transform.position + force, Color.red);
            _lastSpringLength = springNow;

            var pos = _wheelGraphic.localPosition;
            pos.y = -_lastSpringLength + WheelRadius;
            _wheelGraphic.localPosition = pos;
        }
    }
}