using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets
{
    public class FlyOffOnImpact : MonoBehaviour
    {
        private bool _dead = false;
        private Rigidbody _rigidbody;

        private MeshCollider[] _colliders;

        // Use this for initialization
        void Start()
        {
            _colliders = GetComponentsInChildren<MeshCollider>();

            foreach (var collider in _colliders)
            {
                collider.gameObject.layer = LayerMask.NameToLayer("Sign");
                collider.convex = true;
                collider.isTrigger = true;
            }

            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody == null)
                _rigidbody = gameObject.AddComponent<Rigidbody>();

            _rigidbody.mass = 1;
            _rigidbody.isKinematic = true;
        }

        // Update is called once per frame
        void Update()
        {

        }

        void OnTriggerEnter(Collider collider)
        {
            if (_dead)
            {
                return;
            }

            Rigidbody rigidBody = collider.gameObject.GetComponentInParent<Rigidbody>();
            if (rigidBody != null && rigidBody.velocity.magnitude > 2)
            {
                _dead = true;
                foreach (var c in _colliders)
                {
                    c.isTrigger = false;
                    c.gameObject.layer = 0;
                }
                _rigidbody.isKinematic = false;
                _rigidbody.AddForce(Vector3.up * 500);
            }
        }
    }
}