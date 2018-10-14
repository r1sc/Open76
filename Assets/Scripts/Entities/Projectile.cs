using Assets.Scripts.Car;
using UnityEngine;

namespace Assets.Scripts.Entities
{
    public class Projectile : MonoBehaviour
    {
        public float Velocity { get; set; }
        public float Damage { get; set; }
        public Transform Owner { get; set; }

        private const float MaxLifeTime = 10.0f;

        private float _lifeTime;
        
        private void Update()
        {
            float dt = Time.deltaTime;

            if (Velocity > 0.0f)
            {
                transform.Translate(Vector3.forward * Velocity * dt, Space.Self);
            }

            _lifeTime += dt;
            if (_lifeTime > MaxLifeTime)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            Transform parent = other.transform;
            while (parent.parent != null)
            {
                parent = parent.parent;
            }

            if (parent == Owner)
            {
                return;
            }

            CarController car = other.GetComponentInParent<CarController>();
            if (car != null)
            {
                Vector3 hitNormal = (transform.position - other.transform.position).normalized;
                car.ApplyDamage(DamageType.Projectile, hitNormal, (int)Damage);
            }
            // TODO: Deal damage.
            // TODO: Spawn impact sprite.
            Destroy(gameObject);
        }
    }
}
