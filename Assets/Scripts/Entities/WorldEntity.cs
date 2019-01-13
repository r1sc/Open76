using UnityEngine;

namespace Assets.Scripts.Entities
{
    public abstract class WorldEntity : MonoBehaviour
    {
        public int Id { get; set; }
        public abstract bool Alive { get; }
        public int MaxAttackers { get; set; }

        public virtual void ApplyDamage(DamageType damageType, Vector3 hitNormal, int damage)
        {
        }
    }
}
