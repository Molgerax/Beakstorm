using System;
using UltEvents;
using UnityEngine;
using UnityEngine.Pool;

namespace Beakstorm.Gameplay.Projectiles
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private int defaultCapacity = 16;
        [SerializeField] private int maximumCapacity = 16;
        
        [SerializeField] private UltEvent onSpawn;
        [SerializeField] private UltEvent onDetach;
        
        private IObjectPool<Projectile> _objectPool;
        public IObjectPool<Projectile> ObjectPool { set => _objectPool = value; }

        private Transform _poolTransform;
        public Transform PoolTransform { set => _poolTransform = value; }

        [NonSerialized] public bool Released;
        
        public int DefaultCapacity => defaultCapacity;
        public int MaximumCapacity => maximumCapacity;
        
        public void Spawn()
        {
            onSpawn?.Invoke();
        }

        public virtual void OnGetFromPool()
        {
            gameObject.SetActive(true);
        }

        public virtual void OnReleaseToPool()
        {
            gameObject.SetActive(false);
        }
        
        public void Deactivate()
        {
            if (Released)
                return;
            _objectPool.Release(this);
        }

        public void Detach()
        {
            ReParentToPoolTransform();
            onDetach?.Invoke();
        }
        
        public void ReParentToPoolTransform()
        {
            transform.SetParent(_poolTransform, true);
        }
    }
}
