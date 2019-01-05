using Assets.Scripts.Entities;
using UnityEngine;

namespace Assets.Scripts.Entities
{
    public class Sign : WorldEntity
    {
        private bool _dead;
        private Rigidbody _rigidbody;

        private MeshCollider[] _colliders;

        public override bool Alive
        {
            get { return !_dead; }
        }

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