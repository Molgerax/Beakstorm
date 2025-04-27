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
        
        private IObjectPool<Projectile> _objectPool;
        public IObjectPool<Projectile> ObjectPool { set => _objectPool = value; }

        public int DefaultCapacity => defaultCapacity;
        public int MaximumCapacity => maximumCapacity;
        
        public void Spawn()
        {
            onSpawn?.Invoke();
        }
        
        public void Deactivate()
        {
            _objectPool.Release(this);
        }
    }
}
