using UnityEngine;

namespace Assets.Scripts.Entities
{
    public abstract class WorldEntity : MonoBehaviour
    {
        public int Id { get; set; }
        public abstract bool Alive { get; }
        public int MaxAttackers { get; set; }
    }
}
