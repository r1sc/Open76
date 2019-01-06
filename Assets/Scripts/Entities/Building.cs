using Assets.Fileparsers;
using Assets.Scripts.CarSystems;
using Assets.System;
using UnityEngine;

namespace Assets.Scripts.Entities
{
    public class Building : WorldEntity
    {
        private int _health;
        private Sdf _sdf;
        private GameObject _wreckedObject;

        public override bool Alive
        {
            // TODO: implement damage etc.
            get { return _health > 0; }
        }

        public void Initialise(Sdf sdf, GameObject wreckedObject)
        {
            _sdf = sdf;
            _health = (int)sdf.Health;
            _wreckedObject = wreckedObject;
        }

        public override void ApplyDamage(DamageType damageType, Vector3 hitNormal, int damage)
        {
            bool alive = Alive;
            _health -= damage;

            if (alive && _health <= 0)
            {
                if (_sdf.Xdf != null)
                {
                    // TODO: Perform effect.
                }

                if (!string.IsNullOrEmpty(_sdf.DestroySoundName))
                {
                    AudioSource source = CacheManager.Instance.GetAudioSource(gameObject, _sdf.DestroySoundName);
                    if (source != null)
                    {
                        source.Play();
                    }
                }

                if (_wreckedObject != null)
                {
                    foreach (Transform child in transform)
                    {
                        child.gameObject.SetActive(false);
                    }

                    _wreckedObject.SetActive(true);
                }
            }
        }
    }
}
