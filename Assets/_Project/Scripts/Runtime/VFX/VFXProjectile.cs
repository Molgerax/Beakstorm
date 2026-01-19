using System;
using Beakstorm.Gameplay.Projectiles;
using UnityEngine;
using UnityEngine.VFX;

namespace Beakstorm.VFX
{
    public class VFXProjectile : Projectile
    {
        [SerializeField] private VisualEffect visualEffect;

        private void Reset()
        {
            visualEffect = GetComponent<VisualEffect>();
        }

        private void Awake()
        {
            visualEffect.Stop();
        }

        public override void OnGetFromPool()
        {
            visualEffect.Play();
        }

        public override void OnReleaseToPool()
        {
            visualEffect.Stop();
        }
    }
}