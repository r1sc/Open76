using Assets.Scripts.CarSystems;
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
            WorldEntity entity = other.GetComponentInParent<WorldEntity>();
            if (entity != null)
            {
                Transform entityTransform = entity.transform;
                if (entityTransform == Owner)
                {
                    return;
                }

                Vector3 hitNormal = (entityTransform.position - transform.position).normalized;
                entity.ApplyDamage(DamageType.Projectile, hitNormal, (int)Damage);
            }

            // TODO: Spawn impact sprite.
            Destroy(gameObject);
        }
    }
}
