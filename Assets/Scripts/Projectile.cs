using UnityEngine;

namespace Assets.Scripts
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

            // TODO: Deal damage.
            // TODO: Spawn impact sprite.
            Destroy(gameObject);
        }
    }
}
